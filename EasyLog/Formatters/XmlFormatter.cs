using System.Xml;
using System.Xml.Serialization;
using System.Text;
using EasyLog.Exceptions;
using EasyLog.Abstractions;

namespace EasyLog.Formatters;

public class XmlFormatter<T> : ILogFormatter<T> where T : class
{
    private readonly XmlSerializer _serializer;
    private readonly XmlWriterSettings _writerSettings;
    private readonly XmlReaderSettings _readerSettings;
    
    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="indent">Si true, indente le XML pour lisibilité</param>
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
    /// Convertit un objet T en XML
    /// LOGIQUE:
    /// 1. Crée un StringWriter
    /// 2. Crée un XmlWriter avec les settings d'indentation
    /// 3. Sérialise l'objet via XmlSerializer
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
    /// Convertit une collection en XML
    /// LOGIQUE:
    /// 1. Créé un élément racine <ArrayOf{TypeName}>
    /// 2. Sérialise chaque élément comme enfant de la racine
    /// 3. Exemple: <ArrayOfBackupLogEntry><BackupLogEntry>...</><BackupLogEntry>...</></ArrayOfBackupLogEntry>
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
    /// Parse une chaîne XML en objet T
    /// LOGIQUE:
    /// 1. Crée un StringReader
    /// 2. Crée un XmlReader
    /// 3. Désérialise avec XmlSerializer
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
    /// Parse un XML contenant une collection en IEnumerable<T>
    /// LOGIQUE:
    /// 1. Détecte si c'est un élément racine de collection (ArrayOf...)
    /// 2. Désérialise en List<T>
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