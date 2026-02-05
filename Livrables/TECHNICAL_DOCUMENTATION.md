# EasySave - Technical Documentation

## Software Architecture

### Overview
EasySave is a .NET 8.0 console application using Terminal.Gui for the user interface, structured according to a modular 3-layer architecture:
- **EasyLog.dll** : Reusable logging library
- **Services** : Business logic (BackupManager, FileTransferService, StateWriter)
- **View** : User interface (ConsoleUI + 10 specialized classes)

### Main Components

**BackupManager** (`Services.Managers.BackupManager`)
- Manages backup job lifecycle (CRUD)
- Limit: Maximum 5 simultaneous jobs
- Persistence: `jobs.json` (JSON serialization via System.Text.Json)
- Thread-safety: Not thread-safe, sequential use only

**FileTransferService** (`EasySave.Services.FileTransferService`)
- Executes file transfers (File.Copy with overwrite)
- Supports mixed sources (files and directories)
- Preliminary calculation of totals (TotalFiles, TotalSize) before execution
- Differential management: Compares File.GetLastWriteTime()

**StateWriter** (`Services.Writers.StateWriter`)
- Real-time state updates in `state.json`
- Call: `UpdateJobState(BackupJob job)` after each operation
- Format: JSON with indentation (WriteIndented: true)

**DailyJsonLogger / DailyXmlLogger** (`EasyLog.Loggers`)
- Automatic rotation: One file per day (format YYYY-MM-DD.json/xml)
- Method `UpdateDailyPath()` : Checks and updates path on each write
- Thread-safe: lock(_lock) in LoggerBase
- JSON format: JSON Lines (one entry per line)

---

## Data Structures

### BackupJob (Models.BackupJob)
```csharp
public class BackupJob
{
    public string Id { get; set; }                    // Unique GUID
    public string Name { get; set; }                  // Job name (unique)
    public List<string> SourcePath { get; set; }      // 1-5 sources
    public string TargetPath { get; set; }            // Destination
    public BackupType BackupType { get; set; }        // COMPLETE | DIFFERENTIAL
    public BackupState State { get; set; }            // PENDING | ACTIVE | COMPLETED | ERROR
    public DateTime LastExecution { get; set; }       // Last execution
    public long TotalFiles { get; set; }              // Total number of files
    public long TotalSize { get; set; }               // Total size (bytes)
    public long RemainingFiles { get; set; }          // Remaining files
    public long RemainingSize { get; set; }           // Remaining size
    public string? CurrentSourceFile { get; set; }    // Current source file
    public string? CurrentTargetFile { get; set; }    // Current target file
    public float Progress { get; set; }               // Progress (0-100)
}
```

### BackupLogEntry (Models.Entries.BackupLogEntry)
```csharp
public class BackupLogEntry
{
    public DateTime Timestamp { get; set; }
    public string BackupName { get; set; }
    public string SourcePath { get; set; }
    public string TargetPath { get; set; }
    public long FileSize { get; set; }
    public long TransferTime { get; set; }            // Milliseconds (-1 if error)
}
```

---

## Execution Flow

### Job Creation
```
JobCreationWizard.Start()
  → ShowStepName() → Uniqueness validation via GetJobByName()
  → ShowStepSources() → AddSourceForm() loop (max 5)
  → ShowStepDestination() → Path validation
  → ShowStepBackupType() → Selection COMPLETE/DIFFERENTIAL
  → ShowStepValidation() → Summary display
  → CreateJob()
    → BackupManager.CreateJob(name, sources, dest, type)
      → new BackupJob(params) → GUID generation
      → _jobs.Add(job)
      → SaveJob(job)
        → LoadJobsFromFile() → Read jobs.json
        → jobs.Add(job) → Add new job
        → SaveJobsToFile() → Atomic write via temporary file
        → LoadJobs() → Synchronize _jobs list
```

### Job Execution
```
BackupManager.ExecuteJob(jobId)
  → GetJob(jobId) → Retrieve job
  → Calculate TotalFiles/TotalSize for all sources
    → Directory.GetFiles(source, "*", SearchOption.AllDirectories)
    → Sum of FileInfo.Length
  → job.RemainingFiles = TotalFiles
  → job.RemainingSize = TotalSize
  → StateWriter.UpdateJobState(job) → Write state.json
  → For each source:
    → If Directory: FileTransferService.TransferDirectory()
      → GetAllFiles(sourceDir) → Recursive
      → For each file:
        → ShouldCopyFile() → Check backup type
          → COMPLETE: return true
          → DIFFERENTIAL: Compare LastWriteTime
        → If copy: TransferFile()
          → CreateDirectoryStructure(targetDir)
          → Stopwatch.Start()
          → File.Copy(source, dest, overwrite: true)
          → Stopwatch.Stop()
          → Logger.Log(BackupLogEntry) → Append logs/YYYY-MM-DD.json
          → job.RemainingFiles--
          → job.RemainingSize -= fileSize
          → job.UpdateProgress() → (TotalSize - RemainingSize) * 100 / TotalSize
          → StateWriter.UpdateJobState(job)
        → Else: job.RemainingFiles--, job.RemainingSize -= fileSize
    → If File: TransferFile(sourcePath, targetFile, job)
  → job.MarkAsCompleted()
    → LastExecution = DateTime.Now
    → RemainingFiles = 0, RemainingSize = 0
    → Progress = 100, State = COMPLETED
  → StateWriter.UpdateJobState(job)
```

---

## Persistence and Serialization

### jobs.json Format
```json
[
  {
    "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "Name": "Documents",
    "SourcePath": ["C:\\Users\\Matheo\\Documents"],
    "TargetPath": "D:\\Backup\\Documents",
    "BackupType": 0,
    "State": 2,
    "LastExecution": "2026-02-04T14:32:15.123Z",
    "TotalFiles": 1542,
    "TotalSize": 524288000,
    "RemainingFiles": 0,
    "RemainingSize": 0,
    "CurrentSourceFile": null,
    "CurrentTargetFile": null,
    "Progress": 100.0
  }
]
```

### state.json Format
```json
[
  {
    "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "Name": "Documents",
    "State": 1,
    "Progress": 45.5,
    "TotalFiles": 1542,
    "RemainingFiles": 840,
    "CurrentSourceFile": "\\\\?\\C:\\Users\\Matheo\\Documents\\report.pdf",
    "CurrentTargetFile": "\\\\?\\D:\\Backup\\Documents\\report.pdf"
  }
]
```

### logs/2026-02-04.json Format (JSON Lines)
```json
{"Timestamp":"2026-02-04T14:32:15.123Z","BackupName":"Documents","SourcePath":"\\\\?\\C:\\Users\\Matheo\\Documents\\file1.txt","TargetPath":"\\\\?\\D:\\Backup\\Documents\\file1.txt","FileSize":2048,"TransferTime":15}
{"Timestamp":"2026-02-04T14:32:15.456Z","BackupName":"Documents","SourcePath":"\\\\?\\C:\\Users\\Matheo\\Documents\\file2.pdf","TargetPath":"\\\\?\\D:\\Backup\\Documents\\file2.pdf","FileSize":524288,"TransferTime":125}
```

---

## Command-Line Arguments

### Syntax
```
EasySave.exe [pattern]
```

### Parsing (Program.ParseJobIndices)
**Dash pattern (range):** `1-3`
```csharp
var parts = arg.Split('-');
int start = int.Parse(parts[0]);  // 1
int end = int.Parse(parts[1]);    // 3
for (int i = start; i <= end; i++)
    indices.Add(i);
// Result: [1, 2, 3]
```

**Semicolon pattern (list):** `1;3;5`
```csharp
var parts = arg.Split(';');
foreach (var part in parts)
    indices.Add(int.Parse(part.Trim()));
// Result: [1, 3, 5]
```

**Simple pattern:** `2`
```csharp
indices.Add(int.Parse(arg));
// Result: [2]
```

### Execution (Program.ExecuteJobsByIndices)
```
1. Retrieval: var allJobs = BackupManager.GetAllJobs()
2. Validation: arrayIndex = index - 1 (1-based → 0-based conversion)
3. Bounds check: if (arrayIndex < 0 || arrayIndex >= allJobs.Count)
4. Execution: BackupManager.ExecuteJob(job.Id)
5. Display: Console with colors (Cyan/Green/Red)
6. Exit code: 0 if success, 1 if error
```

---

## Configuration and Paths

### System Paths
```csharp
string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
// Windows: C:\Users\[User]\AppData\Roaming

string easySavePath = Path.Combine(appData, "EasySave");
// C:\Users\[User]\AppData\Roaming\EasySave

string logsDir = Path.Combine(easySavePath, "logs");
// C:\Users\[User]\AppData\Roaming\EasySave\logs

string stateFile = Path.Combine(easySavePath, "state.json");
string jobsFile = "./Datas/jobs.json";  // Relative to executable
```

### Initialization (Program.InitializeServices)
```csharp
LocalizationService _localizationService = new LocalizationService("fr");

ILogger logger = new DailyJsonLogger(logsDir);
StateWriter stateWriter = new StateWriter(stateFile);
FileTransferService fileTransferService = new FileTransferService(logger, stateWriter);
BackupManager backupManager = new BackupManager(fileTransferService, stateWriter, maxJobs: 5);

ConsoleUI consoleUI = new ConsoleUI(_localizationService, backupManager);
```

---

## Error Handling

### BackupManager
- **InvalidOperationException** : 5 jobs limit reached
- **ArgumentException** : Job name already exists, job not found
- **IOException** : Error reading/writing jobs.json
- **AggregateException** : ExecuteAll() with multiple failures

### FileTransferService
- **DirectoryNotFoundException** : Source does not exist
- **InvalidOperationException** : Source is not a directory
- **UnauthorizedAccessException** : Access denied
- **FileTransferException** : File copy error (wraps IOException)

### UI Error Handling
```csharp
try {
    _backupManager.ExecuteJob(jobId);
    MessageBox.Query("Success", "Job completed!", "OK");
}
catch (Exception ex) {
    job.MarkAsError();  // State = ERROR, Progress = 0
    _stateWriter.UpdateJobState(job);
    MessageBox.ErrorQuery("Error", ex.Message, "OK");
}
```

---

## Internationalization

### LocalizationService
```csharp
public class LocalizationService
{
    private Dictionary<string, Dictionary<string, string>> _translations;
    private string _currentLanguage;
    
    public string GetTextTranslated(string key);
    public void ChangeLanguage(string languageCode);  // "fr" | "en"
}
```

### translations.json Format
```json
{
  "fr": {
    "menu_create_task": "Créer une tâche",
    "error_no_tasks_available": "Aucune tâche disponible."
  },
  "en": {
    "menu_create_task": "Create a task",
    "error_no_tasks_available": "No tasks available."
  }
}
```

### Usage
```csharp
private string T(string key, params object[] args)
{
    var text = _localizationService.GetTextTranslated(key);
    return args.Length > 0 ? string.Format(text, args) : text;
}

Console.WriteLine(T("error_no_tasks_available"));
// FR: "Aucune tâche disponible."
// EN: "No tasks available."
```

---

## Implemented Design Patterns

1. **Singleton** (ConfigurationManager) - Unique configuration instance
2. **Strategy** (ILogFormatter, JsonFormatter, XmlFormatter) - Format interchangeability
3. **Template Method** (LoggerBase) - Algorithmic skeleton with abstract methods
4. **Dependency Injection** (Constructors) - Inversion of control
5. **Repository** (BackupManager) - Persistence abstraction
6. **Facade** (ConsoleUI) - Simplified interface for complex subsystems
7. **Single Responsibility** (10 UI classes) - One responsibility per class
8. **Observer** (StateWriter) - State change notifications
9. **Factory Method** (GetFormatter) - Instance creation based on configuration
10. **Command** (CLI arguments) - Executable action encapsulation

---

## Performance and Optimization

### I/O Operations
- **Atomic write** : jobs.json written via temporary file + Move
- **Append-only logs** : File.AppendAllText (no prior reading)
- **Preliminary calculation** : TotalFiles/TotalSize before transfer (avoids recalculation)
- **Formatter cache** : Dictionary<Type, object> in LoggerBase

### Memory
- **Limited list** : Maximum 5 jobs in memory (_jobs list)
- **Streaming** : File.Copy in chunks (managed by .NET)
- **No buffering** : Direct write to logs (no caching)

### Thread-Safety
- **Logger** : lock(_lock) in LoggerBase.Log()
- **BackupManager** : Not thread-safe, sequential use only
- **StateWriter** : No lock (called sequentially by single thread)

---

## Limits and Technical Constraints

### Current Constraints
- Maximum 5 simultaneous jobs (arbitrary limit in BackupManager)
- Maximum 5 sources per job (limit in JobCreationWizard)
- Sequential execution only (no parallelism)
- Files < 2GB (File.Copy limitation without custom streaming)
- Paths < 260 characters (Windows limitation, use of \\\\?\\ in logs)

### External Dependencies
- **.NET 8.0** : Required runtime
- **Terminal.Gui** : Console interface (NuGet)
- **System.Text.Json** : Native .NET serialization

---

## Testing and Validation

### Recommended Test Scenarios
1. **Job creation** : Name uniqueness validation, 5 jobs limit
2. **COMPLETE execution** : Copy all files, log verification
3. **DIFFERENTIAL execution** : Copy only modified (LastWriteTime)
4. **Mixed sources** : Files + directories in same job
5. **Log rotation** : Verify creation of logs/YYYY-MM-DD.json at midnight
6. **CLI arguments** : Patterns 1-3, 1;3, 2
7. **Error handling** : Non-existent source, access denied, disk full
8. **Job modification** : Persistence in jobs.json after SaveJob()
9. **StateWriter** : Real-time update during execution
10. **Language change** : Complete interface translation

---

**Version:** 1.0 | **Date:** February 2026 | **Author:** EasySave Team
