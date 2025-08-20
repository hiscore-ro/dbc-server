# DBC Server

A high-performance, cross-platform server that exposes dBase (.DBF) files through a RESTful API with Windows installer and auto-update support.

## Overview

DBC Server provides a high-performance RESTful API interface to read and query dBase database files. Built with ASP.NET Core 8, it runs on Linux, Windows, and macOS, offering a modern way to access legacy dBase data. Successfully tested with large DBF files (100+ MB with 239,000+ records).

## Features

- 🚀 **High Performance**: Optimized for large DBF files (100+ MB, 200,000+ records) with smart caching
- 🔄 **Auto-Updates**: Automatic updates on Windows via Squirrel.Windows  
- 📦 **Easy Installation**: Windows installer with simple configuration via config.json
- ⚡ **Smart Caching**: 15-minute TTL cache with background refresh for instant responses
- 🌐 **Cross-Platform**: Runs on Windows, Linux, and macOS
- 🔧 **Flexible Configuration**: Support for config.json (Windows) and environment variables (Linux/Dev)
- 📄 **Pagination**: Efficient pagination for large datasets
- 🔍 **Search**: Fast barcode-based search with result limiting
- 📊 **RESTful API**: Clean REST API with OpenAPI/Swagger documentation
- 🏗️ **Clean Architecture**: SOLID principles with comprehensive test coverage
- 🐳 **Docker Support**: Containerized deployment option
- 🛠️ **Schema Extraction**: Built-in tool for extracting DBF schemas

## Installation

### Windows (Recommended)

1. Download the latest `Setup.exe` from [Releases](https://github.com/hiscore-ro/dbc-server/releases)
2. Run the installer - the app will be installed to `%LOCALAPPDATA%\DbcServer`
3. Edit `config.json` in the installation directory to configure:
   - Path to your DBF files
   - Server port and URL
   - Auto-update settings
4. The server will auto-update when new versions are available

### Manual Installation (Development)

#### Prerequisites
- .NET 8.0 SDK or later
- Git

#### Steps

1. Clone the repository:
```bash
git clone https://github.com/hiscore-ro/dbc-server.git
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

## Configuration

### Windows (config.json)

After installation, edit `%LOCALAPPDATA%\DbcServer\config.json`:

```json
{
  "dbfPath": "C:\\path\\to\\dbf\\files",
  "serverUrl": "http://localhost:3000",
  "environment": "Production",
  "cacheTtlMinutes": 15,
  "maxSearchResults": 100,
  "updateSettings": {
    "enableAutoUpdate": true,
    "checkIntervalMinutes": 60,
    "updateUrl": "https://github.com/hiscore-ro/dbc-server"
  }
}
```

### Linux/Development (.env)

Create a `.env` file in the project root:

```bash
DBF_PATH=tmp                              # Path to DBF files
ASPNETCORE_URLS=http://localhost:3000    # Server URL
ASPNETCORE_ENVIRONMENT=Development       # Environment
```

### appsettings.json (Optional)

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

## API Endpoints

### Stock Endpoints

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
      "warehouse": 1
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

## Performance

The server is optimized for handling large DBF files with multiple caching layers:

### Optimization Techniques

1. **Singleton Repository Pattern**: Repository registered as singleton to enable cross-request caching
2. **Cached Total Count (15-minute TTL)**: 
   - Total record count cached for 15 minutes to avoid counting 239k+ records on every request
   - Background refresh: When cache is within 2 minutes of expiry, a background thread refreshes it
   - Previous cached value is used while background recalculation happens
3. **Cached Column Ordinals**: Column ordinals cached per file using ConcurrentDictionary
4. **Efficient Pagination**: Skips directly to needed page without reading all preceding records
5. **Selective Field Mapping**: 
   - List endpoints load only essential fields (8 fields)
   - Detail endpoints load all fields (38 fields) with `loadAllFields: true`
6. **Limited Search Results**: Search operations limited to 100 results to prevent memory issues

### Benchmarks

- **Before optimization**: ~1.5-3 seconds per paginated request
- **After optimization**: 
  - First request: ~2.8 seconds (includes cache population)
  - Subsequent requests: ~10-14ms (100-300x faster)
  - Search endpoint: ~180ms
- Successfully handles 100+ MB DBF files with 239,619 records
- Tested with 44 columns per record

## Data Directory

Place your dBase files in the `tmp/` directory:
- `.DBF` - Data files (e.g., STOC.DBF with 239,619 records)
- `.MDX` - Multiple index files
- `.CDX` - Compound index files
- `.FPT` - Memo field files
- `.NTX` - Single index files

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
```

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

## CI/CD

The project includes GitHub Actions workflows for:

- **Windows Release Pipeline** - Automated Windows installer builds
  - Triggers on push to `main` or version tags (e.g., `v1.0.0`)
  - Creates Squirrel.Windows installer with auto-update support
  - Publishes releases to GitHub automatically
  - Cleans up old releases (keeps only last 20 versions)
- **CI/CD Pipeline** - Build, test, and publish on multiple platforms
- **Dependency Check** - Weekly scan for outdated and vulnerable packages
- **Security Scanning** - Trivy vulnerability scanning

### Building Windows Installer Locally

```powershell
# Run the build script
.\build-installer.ps1 -Version 1.0.0

# The installer will be in ./Releases/Setup.exe
```

## Docker

Build and run with Docker:

```bash
# Build image
docker build -t dbc-server .

# Run container
docker run -d -p 3000:80 -v $(pwd)/tmp:/app/tmp dbc-server
```

## Project Structure

```
dbc-server/
├── src/
│   ├── DbcServer.Api/           # Web API endpoints and middleware
│   ├── DbcServer.Core/          # Domain models and interfaces
│   ├── DbcServer.Infrastructure/ # Data access layer (DBF reading)
│   └── DbcServer.Application/   # Business logic and services
├── tests/                       # Unit and integration tests
├── tmp/                         # DBF files directory
├── .github/workflows/           # CI/CD pipelines
├── build-installer.ps1          # Local Windows installer build script
└── config.example.json          # Configuration template
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is proprietary software. All rights reserved.

## Support

For issues, questions, or suggestions, please open an issue on [GitHub](https://github.com/hiscore-ro/dbc-server/issues).

## Author

Vasile Buza - vasile@hiscore.ro