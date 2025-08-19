# DBC Server

A cross-platform REST API server for accessing dBase (.DBF) database files.

## Overview

DBC Server provides a RESTful API interface to read and query dBase database files. Built with ASP.NET Core, it runs on Linux, Windows, and macOS, offering a modern way to access legacy dBase data.

## Features

- Cross-platform support (Linux, Windows, macOS)
- RESTful API with OpenAPI/Swagger documentation
- Read-only access to dBase (.DBF) files
- Support for index files (.MDX, .CDX, .NTX)
- Clean architecture with SOLID principles
- Comprehensive test coverage
- Docker support

## Architecture

The project follows Clean Architecture principles with clear separation of concerns:

- **DbcServer.Api** - Web API layer with controllers and middleware
- **DbcServer.Application** - Business logic and application services
- **DbcServer.Core** - Domain models and interfaces
- **DbcServer.Infrastructure** - Data access and external dependencies

## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Git

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/dbc-server.git
cd dbc-server
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Build the solution:
```bash
dotnet build
```

4. Run the server:
```bash
dotnet run --project src/DbcServer.Api
```

The API will be available at `http://localhost:5000` with Swagger documentation at `http://localhost:5000/swagger`.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tables` | List all available DBF files |
| GET | `/api/tables/{name}` | Get table schema |
| GET | `/api/data/{table}` | Read data from specific table |
| GET | `/api/data/{table}/{id}` | Get specific record |

## Data Directory

Place your dBase files in the `tmp/` directory:
- `.DBF` - Data files
- `.MDX` - Multiple index files
- `.CDX` - Compound index files
- `.FPT` - Memo field files
- `.NTX` - Single index files

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test project
dotnet test tests/DbcServer.UnitTests
```

### Code Quality

```bash
# Format code
dotnet format

# Build with warnings as errors
dotnet build /p:TreatWarningsAsErrors=true

# Check for outdated packages
dotnet list package --outdated
```

### Docker

Build and run with Docker:

```bash
# Build image
docker build -t dbc-server .

# Run container
docker run -d -p 5000:80 -v $(pwd)/tmp:/app/tmp dbc-server
```

## CI/CD

The project includes GitHub Actions workflows for:

- **CI/CD Pipeline** - Build, test, and publish on multiple platforms
- **Dependency Check** - Weekly scan for outdated and vulnerable packages
- **Security Scanning** - Trivy vulnerability scanning
- **Automated Releases** - Create releases with compiled binaries

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or suggestions, please open an issue on GitHub.