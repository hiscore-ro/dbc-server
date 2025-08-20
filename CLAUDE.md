# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

dbc-server is a high-performance, cross-platform C# server that exposes dBase database (.DBF) files through a RESTful API with Windows installer and auto-update support. The server works on both Linux and Windows, providing read access to dBase data files. It successfully handles large DBF files (100+ MB with 239,000+ records) with multiple layers of caching, efficient pagination, and background cache refresh for optimal performance.

### Key Features
- **Windows Installer**: Squirrel.Windows-based Setup.exe with auto-update support
- **Cross-Platform**: Runs on Windows, Linux, and macOS
- **High Performance**: Smart caching with 15-minute TTL and background refresh
- **Flexible Config**: Supports both config.json (Windows) and environment variables (Linux/Dev)
- **Verified Accuracy**: API data tested against dbview CLI tool

## Project Structure

```
dbc-server/
├── src/
│   ├── DbcServer.Api/           # ASP.NET Core Web API project
│   │   └── Services/            # Background services (UpdateService)
│   ├── DbcServer.Core/          # Core business logic and domain models
│   │   └── Configuration/       # AppConfiguration model
│   ├── DbcServer.Infrastructure/ # Data access, dBase file reading
│   │   └── Configuration/       # ConfigurationService
│   └── DbcServer.Application/   # Application services and DTOs
├── tests/
│   ├── DbcServer.UnitTests/     # Unit tests
│   └── DbcServer.IntegrationTests/ # Integration tests
├── tmp/                          # dBase database files (.DBF, .MDX)
├── .github/workflows/            # CI/CD pipelines
│   └── windows-release.yml      # Windows installer build & release
├── build-installer.ps1           # Local Windows installer build script
├── config.example.json           # Configuration template
└── DbcServer.sln                 # Solution file
```

## Tools

### dbview
Linux CLI tool for viewing DBF files directly. Useful for verifying API data accuracy:
```bash
# View first records
dbview tmp/STOC.DBF | head -20

# Find specific record by code
dbview tmp/STOC.DBF | grep -A 20 "Cod        : 50479"

# The API data has been verified to match dbview output exactly
```

### extract-schema
Extracts schema from DBF files and generates SQL CREATE TABLE statements.

```bash
# Extract schema from all DBF files in tmp directory (default)
bin/extract-schema

# Extract schema from specific files
bin/extract-schema tmp/STOC.DBF tmp/OTHER.DBF

# Extract to custom output file
bin/extract-schema tmp/STOC.DBF custom-schema.sql

# Extract from different directory
bin/extract-schema data/*.DBF
```

The tool reads DBF header information and MDX index files (if present) to generate a complete SQL schema including field types and indexes. Default output is `config/schema.sql`.

## CI/CD Workflows

### GitHub Actions
The project uses GitHub Actions for continuous integration and deployment:

1. **CI/CD Pipeline** (`.github/workflows/ci.yml`)
   - Tests on Ubuntu and Windows
   - Builds and verifies code quality
   - Publishes Windows binaries
   - Runs on push to main and PRs

2. **Windows Release** (`.github/workflows/windows-release.yml`)
   - Creates Squirrel.Windows installer
   - Triggered on version tags (e.g., `v1.0.0`)
   - Downloads Squirrel.exe from official releases
   - Creates NuGet package and runs releasify
   - Publishes Setup.exe with auto-update support
   - Uploads to GitHub Releases

### Creating a Release
```bash
# Tag a version
git tag v1.0.0
git push --tags

# This triggers the Windows Release workflow which:
# 1. Builds the application
# 2. Creates NuGet package
# 3. Runs Squirrel releasify
# 4. Publishes Setup.exe to GitHub Releases
```

## Development Commands

### Initial Setup
```bash
# Create the solution and projects
dotnet new sln -n DbcServer
dotnet new webapi -n DbcServer.Api -o src/DbcServer.Api
dotnet new classlib -n DbcServer.Core -o src/DbcServer.Core
dotnet new classlib -n DbcServer.Infrastructure -o src/DbcServer.Infrastructure
dotnet new classlib -n DbcServer.Application -o src/DbcServer.Application
dotnet new xunit -n DbcServer.UnitTests -o tests/DbcServer.UnitTests
dotnet new xunit -n DbcServer.IntegrationTests -o tests/DbcServer.IntegrationTests

# Add projects to solution
dotnet sln add src/DbcServer.Api/DbcServer.Api.csproj
dotnet sln add src/DbcServer.Core/DbcServer.Core.csproj
dotnet sln add src/DbcServer.Infrastructure/DbcServer.Infrastructure.csproj
dotnet sln add src/DbcServer.Application/DbcServer.Application.csproj
dotnet sln add tests/DbcServer.UnitTests/DbcServer.UnitTests.csproj
dotnet sln add tests/DbcServer.IntegrationTests/DbcServer.IntegrationTests.csproj
```

### Build Commands
```bash
# Build the entire solution
dotnet build

# Build in release mode
dotnet build -c Release

# Clean and rebuild
dotnet clean && dotnet build

# Format code (auto-fix)
dotnet format

# Format check (CI validation)
dotnet format --verify-no-changes
```

### Run Commands
```bash
# Run the API server (defaults to port 3000)
dotnet run --project src/DbcServer.Api

# Run with bin/server script
bin/server

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/DbcServer.Api

# Run with hot reload
dotnet watch run --project src/DbcServer.Api

# The server loads environment variables from .env file if present
```

### Test Commands
```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/DbcServer.UnitTests

# Run integration tests only
dotnet test tests/DbcServer.IntegrationTests

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Run tests in verbose mode
dotnet test --logger "console;verbosity=detailed"
```

### Code Quality Commands
```bash
# Format code
dotnet format

# Analyze code
dotnet build /p:TreatWarningsAsErrors=true

# Check for outdated packages
dotnet list package --outdated
```

## Architecture Overview

### SOLID Principles Implementation

1. **Single Responsibility**: Each layer has a specific responsibility
   - Api: HTTP endpoints and request/response handling
   - Application: Business logic orchestration
   - Infrastructure: Data access and external dependencies
   - Core: Domain models and interfaces

2. **Open/Closed**: Use interfaces and dependency injection throughout

3. **Liskov Substitution**: All implementations should be substitutable through their interfaces

4. **Interface Segregation**: Keep interfaces focused and specific

5. **Dependency Inversion**: Depend on abstractions, not concrete implementations

### Key Components

**DbcServer.Core**
- Domain entities representing dBase records
- Repository interfaces (e.g., `IDbfRepository`)
- Core business rules and exceptions

**DbcServer.Infrastructure**
- `DbfReader`: Handles reading .DBF files using a library like `DBase.Net` or `DbfDataReader`
- Repository implementations
- File system access for the `tmp` directory

**DbcServer.Application**
- Service interfaces and implementations
- DTOs for API responses
- Mapping between domain entities and DTOs

**DbcServer.Api**
- Controllers for REST endpoints
- Middleware for error handling
- Dependency injection configuration
- Swagger/OpenAPI documentation

## Required NuGet Packages

```xml
<!-- For reading DBF files -->
<PackageReference Include="DbfDataReader" Version="0.5.11" />
<PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.8" />

<!-- For Windows installer and auto-updates -->
<PackageReference Include="Squirrel.Windows" Version="2.0.1" /> <!-- Used for auto-update functionality -->

<!-- For environment variables -->
<PackageReference Include="DotNetEnv" Version="3.1.1" />

<!-- For API -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />

<!-- For testing -->
<PackageReference Include="xunit" Version="2.4.2" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="FluentAssertions" Version="6.12.1" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.11" />

<!-- For coverage -->
<PackageReference Include="coverlet.collector" Version="6.0.2" />
```

## API Endpoints

### Implemented Endpoints

```
GET /api/stock           - Get paginated stock items
  Query params:
  - pageNumber (default: 1)
  - pageSize (default: 10)
  - barcode (optional, filters results)

GET /api/stock/{code}    - Get specific stock item by code

GET /api/stock/search    - Search stock items by barcode
  Query params:
  - barcode (required)
  - limit: 100 results max
```

### Response Format

All endpoints return JSON with English field names:

```json
{
  "items": [...],
  "totalCount": 239618,
  "pageNumber": 1,
  "pageSize": 5,
  "totalPages": 47924,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

## Cross-Platform Considerations

- Use `Path.Combine()` for file paths instead of hardcoded separators
- Use `Directory.GetCurrentDirectory()` or configuration for base paths
- Handle case-sensitive file systems on Linux
- Use async/await for all I/O operations
- Configure Kestrel for both Windows and Linux hosting

## Testing Strategy

**Unit Tests**
- Test individual components in isolation
- Mock external dependencies
- Focus on business logic in Application and Core layers

**Integration Tests**
- Test API endpoints using `WebApplicationFactory`
- Test actual DBF file reading with sample files
- Verify complete request/response cycles

## Data Directory

The `tmp` directory contains the dBase files:
- STOC.DBF - Main stock data file (100+ MB, 239,619 records, 44 columns)
- STOC.MDX - Index file

Ensure the application has read permissions for this directory on both Windows and Linux.

## Configuration

### Windows Production (config.json)
The server creates `config.json` from `config.example.json` on first run:
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

### Linux/Development (.env file)
```bash
DBF_PATH=tmp                              # Path to DBF files
ASPNETCORE_URLS=http://localhost:3000    # Server URL
ASPNETCORE_ENVIRONMENT=Development       # Environment
```

### appsettings.json (Optional)
```json
{
  "DbfPath": "../../tmp"  // Relative path from Api project
}
```

Configuration priority: Environment Variables > config.json > appsettings.json

## Performance Optimizations

1. **Singleton Repository Pattern**: Repository registered as singleton to enable cross-request caching
2. **Cached Total Count (15-minute TTL)**: 
   - Total record count cached for 15 minutes to avoid counting 239k+ records on every request
   - Background refresh: When cache is within 2 minutes of expiry, a background thread refreshes it
   - Previous cached value is used while background recalculation happens
3. **Cached Column Ordinals**: Column ordinals cached per file using ConcurrentDictionary to avoid repeated lookups
4. **Efficient Pagination**: 
   - Skips directly to needed page without reading all preceding records
   - Only maps records needed for current page
5. **Selective Field Mapping**: 
   - List endpoints load only essential fields (8 fields)
   - Detail endpoints load all fields (38 fields) with `loadAllFields: true`
6. **Limited Search Results**: Search operations limited to 100 results to prevent memory issues
7. **Encoding Support**: Uses System.Text.Encoding.CodePages for Windows-1252 encoding support

### Performance Benchmarks
- **Before optimization**: ~1.5-3 seconds per paginated request
- **After optimization**: 
  - First request: ~2.8 seconds (includes cache population)
  - Subsequent requests: ~10-14ms (100-300x faster)
  - Search endpoint: ~180ms

## Field Mapping

The API returns JSON with English field names while the DBF files contain Romanian column names. All mapping is handled in the Infrastructure layer (StockRepository):

- Romanian: COD → English: code
- Romanian: DENUMIRE → English: name
- Romanian: CATEGORIE → English: category
- Romanian: COD_BARE → English: barcode
- Romanian: CANTITATE → English: quantity
- Romanian: PRET → English: price
- And 38 more field mappings...

## Data Verification

The API has been tested and verified against the `dbview` CLI tool to ensure accurate data reading:
- All field values match exactly between dbview output and API responses
- Numeric precision is preserved (prices, quantities)
- Empty fields are correctly handled as empty strings or nulls
- Romanian characters in field names are properly mapped to English

### Verification Example
```bash
# Using dbview
dbview tmp/STOC.DBF | grep "Cod        : 50479"
# Output: Denumire   : BEC, Pret       : 0.3361

# Using API
curl http://localhost:3000/api/stock/50479
# Output: {"name":"BEC","price":0.3361,...}

# Values match exactly ✓
```

## Troubleshooting

### Windows Installer Issues
- If Squirrel.Windows dotnet tool fails: The workflow downloads Squirrel.exe directly from GitHub releases
- Missing icon.ico: The workflow handles this gracefully and continues without icon

### CI/CD Issues
- Format check failures: Run `dotnet format` locally before pushing
- Build warnings: The CI uses `/p:TreatWarningsAsErrors=false` to allow warnings