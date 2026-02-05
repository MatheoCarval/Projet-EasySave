# EasyLog.dll - Professional Logging Library

## Overview

**EasyLog** is a generic, extensible logging library for .NET 8.0 applications. It provides a flexible architecture for logging any type of data to multiple formats (JSON, XML, CSV, etc.) with built-in thread-safety and customizable formatters.

**Version:** 1.1  
**Target Framework:** .NET 8.0  
**Language:** C# 12  
**License:** ProSoft Internal Use

---

## Key Features

- **Generic logging** - Log any type `T` without code duplication
- **Multiple formats** - JSON, XML support out-of-the-box
- **Custom formatters** - Implement `ILogFormatter<T>` for any format
- **Thread-safe** - Built-in locking mechanism for concurrent access
- **Read/Write operations** - Both log and retrieve data
- **Pretty printing** - Human-readable output for Notepad
- **Zero dependencies** - Uses only .NET standard libraries

---

##  Architecture

### Package Structure

```
EasyLog.dll
├── Abstractions/
│   ├── ILogger.cs              # Main logging interface
│   └── ILogFormatter<T>.cs     # Formatter interface
├── Core/
│   └── LoggerBase.cs           # Abstract base implementation
├── Loggers/
│   ├── JsonLogger.cs           # JSON file logger
│   └── XmlLogger.cs            # XML file logger
├── Formatters/
│   ├── JsonFormatter<T>.cs     # JSON serialization
│   └── XmlFormatter<T>.cs      # XML serialization
├── Enums/
│   └── LogFormat.cs            # Supported formats
└── Exceptions/
    ├── LoggerException.cs      # General logging errors
    └── FormatterException.cs   # Formatting errors
```

---

##  Core Interfaces

### ILogger

Main interface for all logging operations.

```csharp
namespace EasyLog.Abstractions;

public interface ILogger
{
    /// <summary>
    /// Logs a single object of type T
    /// </summary>
    void Log<T>(T data) where T : class;
    
    /// <summary>
    /// Logs a collection of objects
    /// More efficient than multiple Log() calls
    /// </summary>
    void LogCollection<T>(IEnumerable<T> data) where T : class;
    
    /// <summary>
    /// Reads all logged entries of type T
    /// </summary>
    IEnumerable<T> ReadLog<T>() where T : class;
    
    /// <summary>
    /// Flushes any buffered data to disk
    /// (Optional - used for buffered implementations)
    /// </summary>
    void Flush();
}
```

### ILogFormatter<T>

Interface for custom data formatters.

```csharp
namespace EasyLog.Abstractions;

public interface ILogFormatter<T>
{
    /// <summary>
    /// Converts a single object to string format
    /// </summary>
    string Format(T data);
    
    /// <summary>
    /// Converts a collection to string format
    /// </summary>
    string FormatCollection(IEnumerable<T> data);
    
    /// <summary>
    /// Parses a string back to object
    /// </summary>
    T Parse(string content);
    
    /// <summary>
    /// Parses a string back to collection
    /// </summary>
    IEnumerable<T> ParseCollection(string content);
}
```

---

## Usage Examples

### Basic Usage - JSON Logger

```csharp
using EasyLog.Abstractions;
using EasyLog.Loggers;

// Create logger with file path
ILogger logger = new JsonLogger("logs/2026-02-03.json");

// Define your data model
public class BackupLogEntry
{
    public DateTime Timestamp { get; set; }
    public string BackupName { get; set; }
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }
    public long FileSize { get; set; }
    public long TransferTime { get; set; }
    
    // REQUIRED: Parameterless constructor for serialization
    public BackupLogEntry() { }
}

// Log a single entry
var entry = new BackupLogEntry
{
    Timestamp = DateTime.Now,
    BackupName = "Backup1",
    SourcePath = "//server/source/file.txt",
    TargetPath = "//backup/target/file.txt",
    FileSize = 1024,
    TransferTime = 150
};

logger.Log(entry);

// Read back logs
var logs = logger.ReadLog<BackupLogEntry>();
foreach (var log in logs)
{
    Console.WriteLine($"{log.Timestamp}: {log.BackupName} - {log.FileSize} bytes");
}
```

**Output file (logs/2026-02-03.json):**
```json
[
  {
    "timestamp": "2026-02-03T10:30:45.123+01:00",
    "backupName": "Backup1",
    "sourcePath": "//server/source/file.txt",
    "targetPath": "//backup/target/file.txt",
    "fileSize": 1024,
    "transferTime": 150
  }
]
```

---

### Advanced Usage - XML Logger

```csharp
using EasyLog.Loggers;
using EasyLog.Enums;

// Create XML logger
ILogger logger = new XmlLogger("logs/backup-log.xml");

// Log multiple entries efficiently
var entries = new List<BackupLogEntry>
{
    new BackupLogEntry { /* ... */ },
    new BackupLogEntry { /* ... */ },
    new BackupLogEntry { /* ... */ }
};

logger.LogCollection(entries);
```

**Output file (logs/backup-log.xml):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<ArrayOfBackupLogEntry>
  <BackupLogEntry>
    <Timestamp>2026-02-03T10:30:45.123+01:00</Timestamp>
    <BackupName>Backup1</BackupName>
    <SourcePath>//server/source/file.txt</SourcePath>
    <TargetPath>//backup/target/file.txt</TargetPath>
    <FileSize>1024</FileSize>
    <TransferTime>150</TransferTime>
  </BackupLogEntry>
  <!-- More entries... -->
</ArrayOfBackupLogEntry>
```

---

### Custom Formatters

```csharp
using EasyLog.Abstractions;
using EasyLog.Core;

// Create custom CSV formatter
public class CsvFormatter<T> : ILogFormatter<T> where T : class
{
    public string Format(T data)
    {
        // Convert object to CSV line
        // Implementation details...
    }
    
    public string FormatCollection(IEnumerable<T> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,BackupName,SourcePath,TargetPath,FileSize,TransferTime");
        
        foreach (var item in data)
        {
            sb.AppendLine(Format(item));
        }
        
        return sb.ToString();
    }
    
    // Parse implementations...
}

// Register custom formatter
var logger = new JsonLogger("logs/data.csv");
logger.RegisterFormatter(new CsvFormatter<BackupLogEntry>());
```
---

### Switching Formats at Runtime

```csharp
using EasyLog.Enums;

ILogger logger = new JsonLogger("logs/data.json");

// Log in JSON format
logger.Log(entry1);

// Switch to XML
logger.SetFormat(LogFormat.XML);
logger.SetOutputPath("logs/data.xml");

// Now logs in XML format
logger.Log(entry2);
```

---

##  Implementation Details
### LoggerBase Abstract Class

Base implementation providing common functionality for all loggers.

```csharp
namespace EasyLog.Core;

public abstract class LoggerBase : ILogger
{
    protected string _outputPath;
    protected LogFormat _format;
    protected readonly object _lock = new object();
    
    protected LoggerBase(string outputPath, LogFormat format = LogFormat.JSON)
    {
        _outputPath = outputPath;
        _format = format;
        EnsureDirectoryExists(_outputPath);
    }
    
    // Template Method pattern
    public void Log<T>(T data) where T : class
    {
        lock (_lock)  // Thread-safe
        {
            var formatter = GetFormatter<T>();
            
            // Read existing data
            var existingData = ReadExistingData<T>(formatter);
            
            // Add new entry
            existingData.Add(data);
            
            // Format and write
            string content = formatter.FormatCollection(existingData);
            WriteToFile(content, _outputPath);
        }
    }
    
    // Abstract methods to be implemented by derived classes
    protected abstract void WriteToFile(string content, string path);
    protected abstract string ReadFromFile(string path);
}
```

**Design Patterns Used:**
-  **Template Method** - `LoggerBase` defines algorithm, subclasses implement details
-  **Strategy** - `ILogFormatter<T>` allows swapping format strategies

---

## Thread Safety

All logger implementations are **thread-safe** by default.

```csharp
// Multiple threads can safely log concurrently
Parallel.For(0, 100, i =>
{
    logger.Log(new BackupLogEntry 
    { 
        BackupName = $"Backup{i}",
        // ...
    });
});
```

**Implementation:**
- Uses `lock` statement on private `_lock` object
- Ensures atomic read-modify-write operations
- Prevents file corruption during concurrent access

---

## ⚙️ Configuration

### File Paths

**❌ Bad Practice:**
```csharp
// Hardcoded paths - NOT recommended
ILogger logger = new JsonLogger("C:/temp/logs/data.json");
```

**✅ Good Practice:**
```csharp
// Use special folders for cross-platform compatibility
string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
string logPath = Path.Combine(appData, "EasySave", "logs", $"{DateTime.Now:yyyy-MM-dd}.json");

// Ensure directory exists
Directory.CreateDirectory(Path.GetDirectoryName(logPath));

ILogger logger = new JsonLogger(logPath);
```

**Result:**
- Windows: `C:\Users\Username\AppData\Roaming\EasySave\logs\2026-02-03.json`
- Linux: `/home/username/.config/EasySave/logs/2026-02-03.json`

---

### Formatter Options

#### JSON Formatter Options

```csharp
using EasyLog.Formatters;

// Custom JSON formatter with options
var formatter = new JsonFormatter<BackupLogEntry>(
    prettyPrint: true,      // Indented output
    paginate: false         // Form feed characters between entries
);

logger.RegisterFormatter(formatter);
```

**Pretty Print = true:**
```json
[
  {
    "timestamp": "2026-02-03T10:30:45",
    "backupName": "Backup1"
  }
]
```

**Pretty Print = false:**
```json
[{"timestamp":"2026-02-03T10:30:45","backupName":"Backup1"}]
```

#### XML Formatter Options

```csharp
var formatter = new XmlFormatter<BackupLogEntry>(
    indent: true    // Indented XML output
);

logger.RegisterFormatter(formatter);
```

---

## 📖 API Reference

### ILogger Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `Log<T>` | `T data` | `void` | Logs a single entry |
| `LogCollection<T>` | `IEnumerable<T> data` | `void` | Logs multiple entries |
| `ReadLog<T>` | None | `IEnumerable<T>` | Reads all entries |
| `Flush` | None | `void` | Flushes buffered data |

### LoggerBase Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `RegisterFormatter<T>` | `ILogFormatter<T>` | `void` | Register custom formatter |
| `SetOutputPath` | `string path` | `void` | Change output file path |
| `SetFormat` | `LogFormat format` | `void` | Change output format |

### ILogFormatter<T> Methods

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `Format` | `T data` | `string` | Serialize single object |
| `FormatCollection` | `IEnumerable<T>` | `string` | Serialize collection |
| `Parse` | `string content` | `T` | Deserialize single object |
| `ParseCollection` | `string content` | `IEnumerable<T>` | Deserialize collection |

---

## Security Considerations

### 1. Path Validation

```csharp
// Prevent directory traversal attacks
public static string SanitizePath(string userInput)
{
    // Remove dangerous characters
    string sanitized = Path.GetFileName(userInput);
    
    // Validate extension
    string extension = Path.GetExtension(sanitized);
    if (extension != ".json" && extension != ".xml")
    {
        throw new SecurityException("Invalid file extension");
    }
    
    return sanitized;
}
```

---

### 2. Sensitive Data

```csharp
// Don't log sensitive information
public class SecureBackupLogEntry
{
    public DateTime Timestamp { get; set; }
    public string BackupName { get; set; }
    
    // Don't serialize password
    [JsonIgnore]
    [XmlIgnore]
    public string Password { get; set; }
}
```

---

### 3. File Permissions

```csharp
// Set restrictive permissions on log files
var fileInfo = new FileInfo(logFile);
var fileSecurity = fileInfo.GetAccessControl();

// Remove inheritance
fileSecurity.SetAccessRuleProtection(true, false);

// Add only current user
var currentUser = WindowsIdentity.GetCurrent();
fileSecurity.AddAccessRule(new FileSystemAccessRule(
    currentUser.User,
    FileSystemRights.FullControl,
    AccessControlType.Allow
));

fileInfo.SetAccessControl(fileSecurity);
```

---
