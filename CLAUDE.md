# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

dbc-server is a cross-platform C# server that exposes dBase database (.DBF) files through a RESTful API. The server should work on both Linux and Windows, providing read access to dBase data files stored in the `tmp` directory.

## Project Structure

```
dbc-server/
├── src/
│   ├── DbcServer.Api/           # ASP.NET Core Web API project
│   ├── DbcServer.Core/          # Core business logic and domain models
│   ├── DbcServer.Infrastructure/ # Data access, dBase file reading
│   └── DbcServer.Application/   # Application services and DTOs
├── tests/
│   ├── DbcServer.UnitTests/     # Unit tests
│   └── DbcServer.IntegrationTests/ # Integration tests
├── tmp/                          # dBase database files (.DBF, .MDX)
└── DbcServer.sln                 # Solution file
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
```

### Run Commands
```bash
# Run the API server
dotnet run --project src/DbcServer.Api

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/DbcServer.Api

# Run with hot reload
dotnet watch run --project src/DbcServer.Api
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
<PackageReference Include="DbfDataReader" Version="*" />

<!-- For API -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="*" />

<!-- For testing -->
<PackageReference Include="xunit" Version="*" />
<PackageReference Include="Moq" Version="*" />
<PackageReference Include="FluentAssertions" Version="*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="*" />

<!-- For coverage -->
<PackageReference Include="coverlet.collector" Version="*" />
```

## API Endpoints Structure

```
GET /api/tables          - List all available DBF files
GET /api/tables/{name}   - Get table schema
GET /api/data/{table}    - Read data from specific table
GET /api/data/{table}/{id} - Get specific record
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
- STOC.DBF - Main data file
- STOC.MDX - Index file

Ensure the application has read permissions for this directory on both Windows and Linux.