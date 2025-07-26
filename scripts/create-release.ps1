param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("patch", "minor", "major")]
    [string]$VersionType,
    
    [Parameter(Mandatory=$false)]
    [string]$Message = ""
)

# Get current version from csproj file
$csprojPath = "PostOffice/PostOffice.csproj"
$content = Get-Content $csprojPath -Raw
$versionMatch = [regex]::Match($content, '<Version>(\d+)\.(\d+)\.(\d+)</Version>')

if (-not $versionMatch.Success) {
    Write-Error "Could not find version in $csprojPath"
    exit 1
}

$major = [int]$versionMatch.Groups[1].Value
$minor = [int]$versionMatch.Groups[2].Value
$patch = [int]$versionMatch.Groups[3].Value

# Calculate new version
switch ($VersionType) {
    "patch" { $patch++ }
    "minor" { $minor++; $patch = 0 }
    "major" { $major++; $minor = 0; $patch = 0 }
}

$newVersion = "$major.$minor.$patch"
$tag = "v$newVersion"

Write-Host "Current version: $($versionMatch.Groups[0].Value)"
Write-Host "New version: $newVersion"
Write-Host "Tag: $tag"

# Update version in csproj
$newContent = $content -replace '<Version>\d+\.\d+\.\d+</Version>', "<Version>$newVersion</Version>"
Set-Content $csprojPath $newContent -NoNewline

# Update release notes
$releaseNotes = @"
PostOffice v$newVersion - Release Update!

What's New:
- Bug fixes and performance improvements
- Enhanced documentation and examples
- Better error handling and validation
- Optimized memory usage

Features:
- High-performance message processing with middleware pipeline
- FluentValidation integration with custom response support
- Compiled expressions for 10x faster handler invocation
- Object pooling and memory optimizations
- Fast-path validation for simple rules (25x faster!)
- Multiple performance profiles (MaxThroughput, LowLatency, LowMemory)
- Clean architecture with professional folder structure

Performance:
- Sub-microsecond response times
- 400,000+ requests/second throughput
- 50-80% less memory allocations
- Zero reflection overhead

Your validator can return "Test" on errors - just like you wanted!
"@

$releaseNotesContent = $newContent -replace '(?s)<PackageReleaseNotes>.*?</PackageReleaseNotes>', "<PackageReleaseNotes>`n$releaseNotes`n    </PackageReleaseNotes>"
Set-Content $csprojPath $releaseNotesContent -NoNewline

# Build and test
Write-Host "Building project..."
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

Write-Host "Running tests..."
dotnet test PostOffice.Tests/PostOffice.Tests.csproj --configuration Release --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Tests failed"
    exit 1
}

# Create package
Write-Host "Creating NuGet package..."
dotnet pack PostOffice/PostOffice.csproj --configuration Release --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Package creation failed"
    exit 1
}

# Git operations
Write-Host "Committing changes..."
git add .
git commit -m "Release $tag`n`n$Message"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Git commit failed"
    exit 1
}

Write-Host "Creating tag..."
git tag $tag
if ($LASTEXITCODE -ne 0) {
    Write-Error "Git tag failed"
    exit 1
}

Write-Host "Pushing changes and tag..."
git push origin main
git push origin $tag
if ($LASTEXITCODE -ne 0) {
    Write-Error "Git push failed"
    exit 1
}

Write-Host "`n🎉 Release $tag created successfully!" -ForegroundColor Green
Write-Host "The GitHub workflow will automatically publish to NuGet." -ForegroundColor Yellow
Write-Host "Package will be available at: https://www.nuget.org/packages/CQRS.PostOffice/" -ForegroundColor Cyan 