# EasySave Documentation

## Overview
EasySave is a backup software solution that provides file transfer and backup management capabilities.

## Architecture

### Project Structure
- **src/**: Source code directory
  - **EasySave**: Main application
  - **EasyLog**: Logging library
  - **EasySave.Tests**: Unit tests
  - **Models/**: Data models and entities
  - **Services/**: Business logic layer
  - **View/**: User interface components
  - **Utilities/**: Helper classes and utilities
  - **Exceptions/**: Custom exception classes

### Key Components

#### EasyLog Library
A custom logging library supporting multiple formats:
- JSON logging
- XML logging
- Extensible formatter system

#### Services
- **FileTransferService**: Handles file copy operations
- **LocalizationService**: Manages multi-language support
- **BackupManager**: Orchestrates backup operations
- **StateWriter**: Persists backup state

#### Models
- **BackupJob**: Represents a backup configuration
- **BackupLogEntry**: Log entry for backup operations
- **StateEntry**: Current state of backup progress

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code

### Building the Project
```bash
cd src
dotnet build
```

### Running Tests
```bash
cd src
dotnet test
```

### Running the Application
```bash
cd src
dotnet run --project EasySave.csproj
```

## Configuration

### Language Support
Languages are configured in `src/Datas/Languages.json`

### Backup Jobs
Backup jobs are stored in `src/Datas/jobs.json`

## Contributing
Please refer to [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License
See [LICENSE](../LICENSE) for details.
