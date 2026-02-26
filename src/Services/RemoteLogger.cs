using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EasyLog.Abstractions;
using Models.Entries;

namespace EasySave.Services;

/// <summary>
/// Logger that forwards backup log entries to a remote CryptoSoft Manager API
/// and falls back transparently to a local logger when the server is unreachable.
/// Log() is non-blocking: entries are queued and sent asynchronously.
/// </summary>
public class RemoteLogger : ILogger
{
    private readonly ILogger _localFallback;
    private readonly string _serverUrl;
    private readonly HttpClient _http;
    /// <summary>
    /// Both mode: write every entry locally AND send remotely.
    /// Remote-only mode: only write locally when the remote send fails.
    /// </summary>
    private readonly bool _alsoWriteLocal;

    /// <summary>
    /// Optional callback invoked on the thread pool after each send attempt.
    /// Parameters: (success, errorMessageOrNull).
    /// </summary>
    public Action<bool, string?>? StatusCallback { get; set; }

    // Bounded channel: if the server is very slow, at most 500 entries are buffered;
    // older ones are dropped (they remain safe on disk if the local fallback wrote them).
    private readonly Channel<object> _queue;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;

    public RemoteLogger(ILogger localFallback, string serverUrl, string apiKey, bool alsoWriteLocal = false)
    {
        _alsoWriteLocal = alsoWriteLocal;
        _localFallback = localFallback ?? throw new ArgumentNullException(nameof(localFallback));
        _serverUrl = (serverUrl ?? throw new ArgumentNullException(nameof(serverUrl))).TrimEnd('/');

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        _queue = Channel.CreateBounded<object>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

        _processingTask = Task.Run(ProcessQueueAsync);
    }

    // ── ILogger ──────────────────────────────────────────────────────────────

    public void Log<T>(T data) where T : class
    {
        // Non-blocking: just enqueue
        _queue.Writer.TryWrite(data);
    }

    public void LogCollection<T>(IEnumerable<T> data) where T : class
    {
        foreach (var item in data) Log(item);
    }

    /// <summary>
    /// Waits up to 5 s for the queue to drain, then flushes the local fallback.
    /// </summary>
    public void Flush()
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (_queue.Reader.Count > 0 && DateTime.UtcNow < deadline)
            Thread.Sleep(50);

        _localFallback.Flush();
    }

    /// <summary>
    /// Reads are served from the local fallback (remote is write-only from this side).
    /// </summary>
    public IEnumerable<T> ReadLog<T>() where T : class => _localFallback.ReadLog<T>();

    // ── Background processing ─────────────────────────────────────────────────

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var item in _queue.Reader.ReadAllAsync(_cts.Token))
            {
                if (item is BackupLogEntry entry)
                {
                    if (_alsoWriteLocal)
                        _localFallback.Log(entry); // Both: always write locally

                    var (sent, error) = await TrySendAsync(entry);
                    StatusCallback?.Invoke(sent, error);
                    if (!sent)
                    {
                        if (!_alsoWriteLocal)
                            _localFallback.Log(entry); // Remote-only: fallback on failure
                        if (error != null)
                            WriteErrorLog($"Failed to send log: {error}");
                    }
                }
                else
                {
                    // Unknown type: log locally so nothing is lost
                    try { _localFallback.Log((dynamic)item); } catch { /* ignore */ }
                }
            }
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (Exception ex)
        {
            WriteErrorLog($"ProcessQueueAsync crashed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to POST the entry to the remote server.
    /// Returns (true, null) on success, or (false, errorMessage) on failure.
    /// </summary>
    private async Task<(bool Sent, string? Error)> TrySendAsync(BackupLogEntry entry)
    {
        try
        {
            // fileName groups entries by day — required by the server
            var fileName = $"jobs_{entry.Timestamp:yyyy-MM-dd}.json";

            // Serialize the entry as the raw payload (server stores it as-is)
            var rawPayload = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = false });

            var payload = new
            {
                fileName,
                message = $"[EasySave] {entry.BackupName} — {Path.GetFileName(entry.TargetPath)}",
                rawPayload,
                payloadFormat = "json"
            };

            var response = await _http.PostAsJsonAsync($"{_serverUrl}/api/logs/file", payload);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var body = await response.Content.ReadAsStringAsync();
            var code = (int)response.StatusCode;
            if (code == 401 || code == 403)
                return (false, $"Accès refusé (HTTP {code}) — vérifiez l'URL et la clé API dans les paramètres.");
            return (false, $"HTTP {code} {response.ReasonPhrase}: {body}");
        }
        catch (TaskCanceledException)
        {
            return (false, $"Request timed out after {_http.Timeout.TotalSeconds}s (server unreachable?)");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // ── Diagnostics ───────────────────────────────────────────────────────────

    private static readonly string _errorLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EasySave", "remote_logger.log");

    private static void WriteErrorLog(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(_errorLogPath, line);
        }
        catch { /* never crash the background task */ }
    }

    // ── Enrollment helper (static, no instance needed) ─────────────────────

    /// <summary>
    /// Enrolls a new agent with the server using a one-time enrollment key.
    /// Returns (true, apiKey, null) on success or (false, null, errorMessage) on failure.
    /// </summary>
    public static async Task<(bool Success, string? ApiKey, string? Error)> EnrollAsync(
        string serverUrl, string enrollmentKey, string agentName)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var body = new
            {
                enrollmentKey,
                agentName,
                description = $"EasySave agent on {Environment.MachineName}",
                machineName = Environment.MachineName,
                version = "1.0.0"
            };

            var response = await http.PostAsJsonAsync(
                $"{serverUrl.TrimEnd('/')}/api/agent/enroll", body);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, null, $"HTTP {(int)response.StatusCode}: {err}");
            }

            var result = await response.Content.ReadFromJsonAsync<EnrollResponse>();
            if (result?.ApiKey == null)
                return (false, null, "Server returned no API key.");

            return (true, result.ApiKey, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private class EnrollResponse
    {
        public string? ApiKey { get; set; }
        public int AgentId { get; set; }
    }
}
