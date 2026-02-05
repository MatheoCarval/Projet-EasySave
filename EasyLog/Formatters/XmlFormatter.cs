using System.Xml;
using System.Xml.Serialization;
using System.Text;
using EasyLog.Exceptions;
using EasyLog.Abstractions;

namespace EasyLog.Formatters;

/// <summary>
/// Formats and parses XML content for generic types T, with support for single objects and collections with proper serialization settings.
/// </summary>
public class XmlFormatter<T> : ILogFormatter<T> where T : class
{
    /// <summary>
    /// XML serializer for type T used for serialization and deserialization operations.
    /// </summary>
    private readonly XmlSerializer _serializer;
    /// <summary>
    /// XML writer settings configured for indented formatting, UTF-8 encoding without BOM, and XML declaration inclusion.
    /// </summary>
    private readonly XmlWriterSettings _writerSettings;
    /// <summary>
    /// XML reader settings configured to ignore whitespace and comments during deserialization.
    /// </summary>
    private readonly XmlReaderSettings _readerSettings;

    /// <summary>
    /// Initializes a new instance of XmlFormatter with configurable XML indentation, UTF-8 encoding without BOM, and standard XML declaration.
    /// </summary>
    public XmlFormatter(bool indent = true)
    {
        _serializer = new XmlSerializer(typeof(T));

        _writerSettings = new XmlWriterSettings
        {
            Indent = indent,
            IndentChars = "  ",
            NewLineChars = "\n",
            Encoding = new UTF8Encoding(false), // Sans BOM
            OmitXmlDeclaration = false
        };

        _readerSettings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        };
    }

    /// <summary>
    /// Converts a single object of type T to formatted XML string using XmlSerializer with configured indentation and encoding.
    /// </summary>
    public string Format(T data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, _writerSettings);

            _serializer.Serialize(xmlWriter, data);
            return stringWriter.ToString();
        }
        catch (InvalidOperationException ex)
        {
            throw new FormatterException($"Failed to format {typeof(T).Name} to XML", ex);
        }
    }

    /// <summary>
    /// Converts a collection of objects to formatted XML string with an automatically generated root element (ArrayOf{TypeName}) containing serialized child elements.
    /// </summary>
    public string FormatCollection(IEnumerable<T> data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        try
        {
            var list = data.ToList();

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, _writerSettings);

            // Créer un wrapper pour la collection
            var listType = typeof(List<T>);
            var listSerializer = new XmlSerializer(listType);

            listSerializer.Serialize(xmlWriter, list);
            return stringWriter.ToString();
        }
        catch (InvalidOperationException ex)
        {
            throw new FormatterException($"Failed to format collection of {typeof(T).Name} to XML", ex);
        }
    }

    /// <summary>
    /// Parses an XML string and deserializes it into a single object of type T using XmlSerializer.
    /// </summary>
    public T Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty", nameof(content));

        try
        {
            using var stringReader = new StringReader(content);
            using var xmlReader = XmlReader.Create(stringReader, _readerSettings);

            var result = (T?)_serializer.Deserialize(xmlReader);
            return result ?? throw new FormatterException($"Deserialization returned null for type {typeof(T).Name}");
        }
        catch (InvalidOperationException ex)
        {
            throw new FormatterException($"Failed to parse XML to {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Parses an XML string containing a collection and deserializes it into an IEnumerable of type T, handling ArrayOf{TypeName} root elements automatically.
    /// </summary>
    public IEnumerable<T> ParseCollection(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Enumerable.Empty<T>();

        try
        {
            using var stringReader = new StringReader(content);
            using var xmlReader = XmlReader.Create(stringReader, _readerSettings);

            var listType = typeof(List<T>);
            var listSerializer = new XmlSerializer(listType);

            var list = (List<T>?)listSerializer.Deserialize(xmlReader);
            return list ?? Enumerable.Empty<T>();
        }
        catch (InvalidOperationException ex)
        {
            throw new FormatterException($"Failed to parse XML collection to {typeof(T).Name}", ex);
        }
    }
}