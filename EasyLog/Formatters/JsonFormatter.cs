namespace EasyLog.Formatters;

using EasyLog.Exceptions;
using EasyLog.Abstractions;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

public class JsonFormatter<T> : ILogFormatter<T> where T : class
{
    private readonly JsonSerializerOptions _options;
    private readonly bool _paginate;

    public JsonFormatter(bool prettyPrint = true, bool paginate = false)
    {
        _paginate = paginate;
        _options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public string Format(T data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            return JsonSerializer.Serialize(data, _options);
        }
        catch (JsonException ex)
        {
            throw new FormatterException($"Failed to format {typeof(T).Name} to JSON", ex);
        }
    }

    public string FormatCollection(IEnumerable<T> data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            var list = data.ToList();

            if (!list.Any())
                return "[]";

            // ✅ SIMPLE : Juste sérialiser la liste complète
            return JsonSerializer.Serialize(list, _options);
        }
        catch (JsonException ex)
        {
            throw new FormatterException($"Failed to format collection of {typeof(T).Name} to JSON: {ex.Message}", ex);
        }
    }

    public T Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        try
        {
            // Nettoyer
            content = content.Replace("\f", "").Trim();

            var result = JsonSerializer.Deserialize<T>(content, _options);

            if (result == null)
                throw new FormatterException($"Deserialization returned null for type {typeof(T).Name}");

            return result;
        }
        catch (JsonException ex)
        {
            throw new FormatterException($"Failed to parse JSON to {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    public IEnumerable<T> ParseCollection(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Enumerable.Empty<T>();

        try
        {
            // Nettoyer
            content = content.Replace("\f", "").Trim();

            var list = JsonSerializer.Deserialize<List<T>>(content, _options);
            return list ?? Enumerable.Empty<T>();
        }
        catch (JsonException ex)
        {
            // ✅ Log l'erreur avec plus de détails
            throw new FormatterException(
                $"Failed to parse JSON collection to {typeof(T).Name}. Content length: {content.Length}. Error: {ex.Message}",
                ex
            );
        }
    }
}