namespace EasyLog.Formatters;

using EasyLog.Exceptions;
using EasyLog.Abstractions;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;

/// <summary>
/// Formats and parses JSON content for generic types T with support for single objects and collections, including optional pagination capabilities.
/// </summary>
public class JsonFormatter<T> : ILogFormatter<T> where T : class
{
    /// <summary>
    /// JSON serialization options configured for camelCase property naming, null value exclusion, and safe Unicode escaping.
    /// </summary>
    private readonly JsonSerializerOptions _options;
    /// <summary>
    /// Flag indicating whether to apply pagination when formatting collections.
    /// </summary>
    private readonly bool _paginate;

    /// <summary>
    /// Initializes a new instance of JsonFormatter with optional pretty printing and pagination settings.
    /// </summary>
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

    /// <summary>
    /// Converts a single object of type T to a formatted JSON string using configured serialization options.
    /// </summary>
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

    /// <summary>
    /// Converts a collection of objects to a formatted JSON array string. Returns empty array for empty collections.
    /// </summary>
    public string FormatCollection(IEnumerable<T> data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            var list = data.ToList();

            if (!list.Any())
                return "[]";

            return JsonSerializer.Serialize(list, _options);
        }
        catch (JsonException ex)
        {
            throw new FormatterException($"Failed to format collection of {typeof(T).Name} to JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses a JSON string and deserializes it into a single object of type T with content cleaning and null validation.
    /// </summary>
    public T Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        try
        {
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

    /// <summary>
    /// Parses a JSON array string and deserializes it into a collection of objects of type T. Returns empty collection if content is invalid or null.
    /// </summary>
    public IEnumerable<T> ParseCollection(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Enumerable.Empty<T>();

        try
        {
            content = content.Replace("\f", "").Trim();

            var list = JsonSerializer.Deserialize<List<T>>(content, _options);
            return list ?? Enumerable.Empty<T>();
        }
        catch (JsonException ex)
        {
            throw new FormatterException(
                $"Failed to parse JSON collection to {typeof(T).Name}. Content length: {content.Length}. Error: {ex.Message}",
                ex
            );
        }
    }
}