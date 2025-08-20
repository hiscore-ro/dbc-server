# Windows Installer Build Script for DbcServer
# This script builds a Squirrel.Windows installer for the application

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [switch]$SkipBuild = $false
)

Write-Host "Building DbcServer Windows Installer v$Version" -ForegroundColor Green

# Check if running on Windows
if (-not $IsWindows -and $PSVersionTable.Platform -ne "Win32NT") {
    Write-Error "This script must be run on Windows"
    exit 1
}

# Build the application if not skipped
if (-not $SkipBuild) {
    Write-Host "Building application..." -ForegroundColor Yellow
    dotnet publish src/DbcServer.Api/DbcServer.Api.csproj `
        -c $Configuration `
        -r win-x64 `
        --self-contained `
        -p:PublishSingleFile=true `
        -p:Version=$Version `
        -o ./publish
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed"
        exit 1
    }
}

# Copy config.example.json to publish directory
Write-Host "Copying configuration files..." -ForegroundColor Yellow
Copy-Item -Path "config.example.json" -Destination "./publish/config.example.json" -Force

# Install Squirrel tools if not present
$squirrelPath = (Get-Command squirrel -ErrorAction SilentlyContinue).Path
if (-not $squirrelPath) {
    Write-Host "Installing Squirrel.Windows tools..." -ForegroundColor Yellow
    dotnet tool install -g squirrel.windows
}

# Create NuSpec file
Write-Host "Creating NuGet package specification..." -ForegroundColor Yellow
$nuspecContent = @"
<?xml version="1.0"?>
<package>
  <metadata>
    <id>DbcServer</id>
    <version>$Version</version>
    <title>DBC Server</title>
    <authors>Vasile Buza</authors>
    <description>Cross-platform DBF file API server with Windows auto-update support</description>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <iconUrl>https://raw.githubusercontent.com/hiscore-ro/dbc-server/main/icon.png</iconUrl>
  </metadata>
  <files>
    <file src="publish\**\*.*" target="lib\net45\" />
  </files>
</package>
"@

$nuspecContent | Out-File -FilePath "DbcServer.nuspec" -Encoding UTF8

# Create NuGet package
Write-Host "Creating NuGet package..." -ForegroundColor Yellow
if (-not (Test-Path "./nupkg")) {
    New-Item -ItemType Directory -Path "./nupkg" | Out-Null
}

nuget pack DbcServer.nuspec -Version $Version -OutputDirectory ./nupkg

if ($LASTEXITCODE -ne 0) {
    Write-Error "NuGet pack failed"
    exit 1
}

# Create Releases directory
if (-not (Test-Path "./Releases")) {
    New-Item -ItemType Directory -Path "./Releases" | Out-Null
}

# Create Squirrel installer
Write-Host "Creating Squirrel installer..." -ForegroundColor Yellow
$nupkgPath = "./nupkg/DbcServer.$Version.nupkg"

# Check if icon exists, otherwise skip it
$iconParam = ""
if (Test-Path "./icon.ico") {
    $iconParam = "--setupIcon ./icon.ico"
}

# Run Squirrel
$squirrelCmd = "squirrel --releasify `"$nupkgPath`" --releaseDir ./Releases --no-msi $iconParam"
Invoke-Expression $squirrelCmd

if ($LASTEXITCODE -ne 0) {
    Write-Error "Squirrel releasify failed"
    exit 1
}

Write-Host "✓ Windows installer created successfully!" -ForegroundColor Green
Write-Host "  Setup.exe location: ./Releases/Setup.exe" -ForegroundColor Cyan
Write-Host "  Full package: ./Releases/DbcServer-$Version-full.nupkg" -ForegroundColor Cyan
Write-Host ""
Write-Host "To test the installer:" -ForegroundColor Yellow
Write-Host "  1. Run ./Releases/Setup.exe" -ForegroundColor White
Write-Host "  2. The app will be installed to %LOCALAPPDATA%\DbcServer" -ForegroundColor White
Write-Host "  3. Edit config.json in the installation directory" -ForegroundColor White
Write-Host "  4. The app will auto-update when new releases are available" -ForegroundColor White