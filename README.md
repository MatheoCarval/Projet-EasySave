# EasySave

A reliable and efficient backup software solution built with .NET 8.0.

[![CI](https://github.com/YOUR-USERNAME/Projet-EasySave/actions/workflows/dotnet.yml/badge.svg)](https://github.com/YOUR-USERNAME/Projet-EasySave/actions/workflows/dotnet.yml)
[![Release](https://github.com/YOUR-USERNAME/Projet-EasySave/actions/workflows/release.yml/badge.svg)](https://github.com/YOUR-USERNAME/Projet-EasySave/actions/workflows/release.yml)

## Features

- 🔄 Multiple backup types (Full, Differential, Incremental)
- 📁 File and directory backup support
- 🌍 Multi-language support
- 📊 State tracking and progress monitoring
- 📝 Comprehensive logging (JSON/XML formats)
- ⚡ High performance file transfer
- 🧪 Extensive test coverage

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows, Linux, or macOS

### Installation

#### From Release (Recommended)

Download the latest release for your platform from the [Releases](https://github.com/YOUR-USERNAME/Projet-EasySave/releases) page.

#### Build from Source

```bash
# Clone the repository
git clone https://github.com/YOUR-USERNAME/Projet-EasySave.git
cd Projet-EasySave

# Build the project
cd src
dotnet build

# Run the application
dotnet run --project EasySave.csproj
```

## Usage

Launch the application and follow the interactive console interface to:

1. Create backup jobs
2. Configure source and destination paths
3. Select backup type (Full/Differential/Incremental)
4. Execute backups
5. Monitor progress and view logs

## Documentation

Comprehensive documentation is available in the [docs](./docs) folder:

- [API Documentation](./docs/API.md)
- [Contributing Guidelines](./docs/CONTRIBUTING.md)
- [Architecture Overview](./docs/README.md)

## Project Structure

```
Projet-EasySave/
├── src/                    # Source code
│   ├── EasySave/          # Main application
│   ├── EasyLog/           # Logging library
│   └── EasySave.Tests/    # Unit tests
├── docs/                  # Documentation
├── .github/               # GitHub workflows
└── README.md             # This file
```

## Development

### Running Tests

```bash
cd src
dotnet test
```

### Code Formatting

```bash
cd src
dotnet format EasySave.slnx
```

### Building for Release

```bash
cd src
dotnet publish EasySave.csproj -c Release -r win-x64 --self-contained
```

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](./docs/CONTRIBUTING.md) for details.

### Development Workflow

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- 📧 Email: your-email@example.com
- 🐛 Issues: [GitHub Issues](https://github.com/YOUR-USERNAME/Projet-EasySave/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/YOUR-USERNAME/Projet-EasySave/discussions)

## Roadmap

- [ ] GUI interface (WPF/Avalonia)
- [ ] Cloud storage integration
- [ ] Scheduled backups
- [ ] Encryption support
- [ ] Compression options
- [ ] Network backup support

## Acknowledgments

- Built with [.NET 8.0](https://dotnet.microsoft.com/)
- Testing with [xUnit](https://xunit.net/)
- CI/CD with [GitHub Actions](https://github.com/features/actions)

---

Made with ❤️ by the EasySave Team
