# DBC Server

A cross-platform REST API server for accessing dBase (.DBF) database files.

## Overview

DBC Server provides a high-performance RESTful API interface to read and query dBase database files. Built with ASP.NET Core 8, it runs on Linux, Windows, and macOS, offering a modern way to access legacy dBase data. Successfully tested with large DBF files (100+ MB with 239,000+ records).

## Features

- Cross-platform support (Linux, Windows, macOS)
- RESTful API with OpenAPI/Swagger documentation
- Read-only access to dBase (.DBF) files with pagination
- Efficient handling of large datasets (100+ MB files)
- English JSON responses with automatic field name translation
- Barcode-based searching with optimized filtering
- Support for index files (.MDX, .CDX, .NTX)
- Clean architecture with SOLID principles
- Comprehensive test coverage (23 tests passing)
- Docker support
- Schema extraction tool for DBF files
- Environment variable configuration (.env support)

## Architecture

The project follows Clean Architecture principles with clear separation of concerns:

- **DbcServer.Api** - Web API layer with controllers and middleware
- **DbcServer.Application** - Business logic and application services
- **DbcServer.Core** - Domain models and interfaces
- **DbcServer.Infrastructure** - Data access and external dependencies
- **ExtractSchema** - Command-line tool for extracting schema from DBF files

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
bin/server
# or
dotnet run --project src/DbcServer.Api
```

The API will be available at `http://localhost:3000` with Swagger documentation at `http://localhost:3000/swagger`.

## API Endpoints

### Stock Endpoints (Implemented)

| Method | Endpoint | Description | Query Parameters |
|--------|----------|-------------|------------------|
| GET | `/api/stock` | Get paginated stock items | `pageNumber`, `pageSize`, `barcode` |
| GET | `/api/stock/{code}` | Get specific stock item by code | - |
| GET | `/api/stock/search` | Search stock items by barcode | `barcode` (required) |

### Example Requests

```bash
# Get first 5 stock items
curl "http://localhost:3000/api/stock?pageSize=5"

# Get page 2 with 20 items per page
curl "http://localhost:3000/api/stock?pageNumber=2&pageSize=20"

# Get specific item by code
curl "http://localhost:3000/api/stock/123"

# Search by barcode
curl "http://localhost:3000/api/stock/search?barcode=ABC123"
```

### Response Format

```json
{
  "items": [
    {
      "code": 123,
      "name": "Product Name",
      "category": "Category",
      "barcode": "1234567890",
      "quantity": 100,
      "price": 29.99,
      "unit": "pcs",
      "warehouse": 1,
      "priceB": 0,
      "priceC": 0,
      "lot": "",
      "warranty": null,
      "notes": ""
    }
  ],
  "totalCount": 239618,
  "pageNumber": 1,
  "pageSize": 5,
  "totalPages": 47924,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

## Data Directory

Place your dBase files in the `tmp/` directory:
- `.DBF` - Data files (e.g., STOC.DBF with 239,619 records)
- `.MDX` - Multiple index files
- `.CDX` - Compound index files
- `.FPT` - Memo field files
- `.NTX` - Single index files

The server successfully handles large DBF files (100+ MB) with efficient pagination and caching.

## Tools

### extract-schema

Extracts database schema from DBF files and generates SQL CREATE TABLE statements:

```bash
# Extract schema from all DBF files in tmp directory (default)
bin/extract-schema

# Extract from specific files
bin/extract-schema tmp/STOC.DBF tmp/OTHER.DBF

# Extract to custom output file
bin/extract-schema tmp/STOC.DBF custom-schema.sql

# Extract from different directory
bin/extract-schema data/*.DBF
```

The tool automatically reads DBF headers and MDX index files to generate complete SQL schemas. Default input is `tmp/*.DBF` and default output is `config/schema.sql`.

## Performance

The server is optimized for handling large DBF files:

- **Cached Column Ordinals**: Column lookups are cached using ConcurrentDictionary to avoid repeated scans
- **Efficient Pagination**: Only requested records are loaded into memory
- **Limited Search Results**: Search operations return max 100 results to prevent memory issues
- **Encoding Support**: Uses System.Text.Encoding.CodePages for Windows-1252 encoding (Romanian characters)

### Benchmarks

- Successfully handles 100+ MB DBF files
- Processes 239,619 records efficiently
- Sub-second response times for paginated queries
- Tested with 44 columns per record

## Development

### Running Tests

```bash
# Run all tests (23 tests)
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test project
dotnet test tests/DbcServer.UnitTests    # 14 unit tests
dotnet test tests/DbcServer.IntegrationTests  # 9 integration tests
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

### Configuration

Create a `.env` file in the project root:

```bash
DBF_PATH=tmp                              # Path to DBF files
ASPNETCORE_URLS=http://localhost:3000    # Server URL
ASPNETCORE_ENVIRONMENT=Development       # Environment
```

Configure `appsettings.json`:

```json
{
  "DbfPath": "../../tmp",  // Relative path from Api project
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Docker

Build and run with Docker:

```bash
# Build image
docker build -t dbc-server .

# Run container
docker run -d -p 3000:80 -v $(pwd)/tmp:/app/tmp dbc-server
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