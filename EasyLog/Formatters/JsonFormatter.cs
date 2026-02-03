namespace EasyLog.Formatters;

using EasyLog.Exceptions;
using EasyLog.Abstractions;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

public class JsonFormatter<T> : ILogFormatter<T> where T : class
{
    private readonly JsonSerializerOptions _options;
    private readonly bool _paginate;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="prettyPrint">Si true, ajoute indentation et retours à la ligne</param>
    /// <param name="paginate">Si true, ajoute des sauts de page entre entrées (pour Notepad)</param>
    public JsonFormatter(bool prettyPrint = true, bool paginate = false)
    {
        _paginate = paginate;
        _options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Pour les caractères spéciaux
        };
    }

    /// <summary>
    /// Convertit un objet T en JSON
    /// LOGIQUE:
    /// 1. Sérialise l'objet avec System.Text.Json
    /// 2. Si paginate=true, ajoute des caractères de saut de page
    /// </summary>
    public string Format(T data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            string json = JsonSerializer.Serialize(data, _options);

            if (_paginate)
            {
                // Ajouter un saut de page (Form Feed) pour Notepad
                json += "\f\n";
            }

            return json;
        }
        catch (JsonException ex)
        {
            throw new FormatterException($"Failed to format {typeof(T).Name} to JSON", ex);
        }
    }

    /// <summary>
    /// Convertit une collection en JSON array
    /// LOGIQUE:
    /// 1. Sérialise toute la liste en un tableau JSON: [obj1, obj2, ...]
    /// 2. Si paginate=true, ajoute des retours à la ligne entre chaque élément
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

            if (_paginate)
            {
                // Format avec séparateurs pour lisibilité dans Notepad
                var sb = new StringBuilder();
                sb.AppendLine("[");

                for (int i = 0; i < list.Count; i++)
                {
                    string itemJson = JsonSerializer.Serialize(list[i], _options);

                    // Indenter chaque ligne de l'objet
                    var lines = itemJson.Split('\n');
                    foreach (var line in lines)
                    {
                        sb.Append("  ").AppendLine(line);
                    }

                    if (i < list.Count - 1)
                        sb.AppendLine(",");

                    // Saut de page entre chaque entrée
                    if (_paginate && i < list.Count - 1)
                        sb.AppendLine("\f");
                }

                sb.AppendLine("]");
                return sb.ToString();
            }
            else
            {
                return JsonSerializer.Serialize(list, _options);
            }
        }
        catch (JsonException ex)
        {
            throw new FormatterException($"Failed to format collection of {typeof(T).Name} to JSON", ex);
        }
    }

    /// <summary>
    /// Parse une chaîne JSON en objet T
    /// LOGIQUE:
    /// 1. Nettoie les caractères de pagination si présents
    /// 2. Désérialise avec System.Text.Json
    /// </summary>
    public T Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        try
        {
            // Nettoyer les caractères de pagination
            content = content.Replace("\f", "").Trim();

            var result = JsonSerializer.Deserialize<T>(content, _options);
            return result ?? throw new FormatterException($"Failed to deserialize JSON to {typeof(T).Name}: result was null");
        }
        catch (JsonException ex)
        {
            throw new FormatterException($"Failed to parse JSON to {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Parse un JSON array en collection d'objets T
    /// LOGIQUE:
    /// 1. Nettoie les caractères de pagination
    /// 2. Désérialise en List<T>
    /// 3. Retourne IEnumerable<T>
    /// </summary>
    public IEnumerable<T> ParseCollection(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Enumerable.Empty<T>();

        try
        {
            // Nettoyer les caractères de pagination
            content = content.Replace("\f", "").Trim();

            var list = JsonSerializer.Deserialize<List<T>>(content, _options);
            return list ?? Enumerable.Empty<T>();
        }
        catch (JsonException ex)
        {
            throw new FormatterException($"Failed to parse JSON collection to {typeof(T).Name}", ex);
        }
    }
}