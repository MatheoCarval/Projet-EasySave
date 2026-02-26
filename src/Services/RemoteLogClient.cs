using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Models.Entries;

namespace EasySave.Services;

/// <summary>
/// Reads log entries from the remote CryptoSoft Manager API (GET /api/logs).
/// </summary>
public class RemoteLogClient
{
    private readonly HttpClient _http;
    private readonly string _serverUrl;

    public RemoteLogClient(string serverUrl, string apiKey)
    {
        _serverUrl = serverUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    /// <summary>
    /// Returns a user-friendly error string for HTTP failures.
    /// 401/403 → clear "API key expired / re-enroll" message instead of raw JSON.
    /// </summary>
    private static string FormatHttpError(int statusCode, string body)
    {
        if (statusCode == 401 || statusCode == 403)
            return $"Accès refusé (HTTP {statusCode}) — vérifiez l'URL et la clé API dans les paramètres.";
        return $"HTTP {statusCode}: {body}";
    }

    /// <summary>
    /// Fetches the list of log file names available on the server (GET /api/logs/files).
    /// Returns file names like "jobs_2026-02-25.json" and an optional error.
    /// </summary>
    public async Task<(List<string> Files, string? Error)> GetLogFilesAsync()
    {
        var url = $"{_serverUrl}/api/logs/files";
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
        }
        catch (Exception ex)
        {
            return (new(), $"Connexion échouée : {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return (new(), FormatHttpError((int)response.StatusCode, err));
        }

        try
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var files = new List<string>();
            // Handle direct array or object with "files" / "data" property
            JsonElement arrayEl;
            if (root.ValueKind == JsonValueKind.Array)
                arrayEl = root;
            else if (root.TryGetProperty("files", out var f))
                arrayEl = f;
            else if (root.TryGetProperty("data", out var d))
                arrayEl = d;
            else
                return (new(), "Format de réponse inattendu");

            foreach (var el in arrayEl.EnumerateArray())
            {
                // Element can be a plain string or an object with a "fileName" property
                if (el.ValueKind == JsonValueKind.String)
                {
                    var name = el.GetString();
                    if (!string.IsNullOrWhiteSpace(name)) files.Add(name);
                }
                else if (el.TryGetProperty("fileName", out var fn))
                {
                    var name = fn.GetString();
                    if (!string.IsNullOrWhiteSpace(name)) files.Add(name);
                }
            }

            return (files, null);
        }
        catch (Exception ex)
        {
            return (new(), $"Erreur de parsing : {ex.Message}");
        }
    }

    /// <summary>
    /// Fetches paginated log entries from the server.
    /// Returns deserialized entries (rawPayload → BackupLogEntry) and an optional error.
    /// </summary>
    public async Task<(List<RemoteLogEntry> Entries, string? Error)> GetLogsAsync(
        string? fileName = null, int page = 1, int pageSize = 200)
    {
        var url = $"{_serverUrl}/api/logs?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(fileName))
            url += $"&fileName={Uri.EscapeDataString(fileName)}";

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
        }
        catch (Exception ex)
        {
            return (new(), $"Connexion échouée : {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return (new(), FormatHttpError((int)response.StatusCode, err));
        }

        try
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Handle direct array or paginated {items:[...]}, {data:[...]}, {logs:[...]}
            JsonElement itemsEl;
            if (root.ValueKind == JsonValueKind.Array)
                itemsEl = root;
            else if (root.TryGetProperty("items", out var items))
                itemsEl = items;
            else if (root.TryGetProperty("data", out var data))
                itemsEl = data;
            else if (root.TryGetProperty("logs", out var logs))
                itemsEl = logs;
            else
                return (new(), "Format de réponse inattendu");

            var result = new List<RemoteLogEntry>();
            foreach (var el in itemsEl.EnumerateArray())
                if (ParseItem(el) is { } entry)
                    result.Add(entry);

            return (result, null);
        }
        catch (Exception ex)
        {
            return (new(), $"Erreur de parsing : {ex.Message}");
        }
    }

    private static RemoteLogEntry? ParseItem(JsonElement el)
    {
        try
        {
            // Try to deserialize rawPayload into a structured BackupLogEntry
            BackupLogEntry? backupEntry = null;
            if (el.TryGetProperty("rawPayload", out var raw) && raw.ValueKind == JsonValueKind.String)
            {
                var rawStr = raw.GetString();
                if (!string.IsNullOrWhiteSpace(rawStr))
                {
                    try
                    {
                        backupEntry = JsonSerializer.Deserialize<BackupLogEntry>(rawStr,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch { /* rawPayload might not be a BackupLogEntry — keep null */ }
                }
            }

            int id = el.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
            string? fileName = el.TryGetProperty("fileName", out var fn) ? fn.GetString() : null;
            string? message = el.TryGetProperty("message", out var msg) ? msg.GetString() : null;

            DateTime? ts = null;
            if (el.TryGetProperty("timestamp", out var tsEl) && tsEl.ValueKind != JsonValueKind.Null)
                ts = tsEl.TryGetDateTimeOffset(out var dt) ? dt.LocalDateTime : null;

            return new RemoteLogEntry
            {
                Id = id,
                FileName = fileName ?? string.Empty,
                Message = message ?? string.Empty,
                ServerTimestamp = ts,
                BackupEntry = backupEntry
            };
        }
        catch { return null; }
    }
}

/// <summary>
/// Raw data returned by the server for one log record.
/// BackupEntry is populated when rawPayload is a valid serialized BackupLogEntry.
/// </summary>
public class RemoteLogEntry
{
    public int Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime? ServerTimestamp { get; init; }
    public BackupLogEntry? BackupEntry { get; init; }
}
