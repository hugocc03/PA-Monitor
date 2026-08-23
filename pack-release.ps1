$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$project = Join-Path $root "src\PAMonitor.XrmToolBox\PAMonitor.XrmToolBox.csproj"
$nuspec = Join-Path $root "src\PAMonitor.XrmToolBox\PAMonitor.XrmToolBox.nuspec"
$dist = Join-Path $root "dist"
$iconScript = Join-Path $root "tools\GeneratePluginIcons.ps1"

if (-not (Test-Path $iconScript)) {
    throw "Icon generator not found: $iconScript"
}

Write-Host "Generating plugin icons..."
& powershell -ExecutionPolicy Bypass -File $iconScript

Write-Host "Building Release..."
dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Release build failed"
}

$dll = Join-Path $root "src\PAMonitor.XrmToolBox\bin\Release\PAMonitor.XrmToolBox.dll"
if (-not (Test-Path $dll)) {
    throw "Release DLL not found: $dll"
}

$version = [System.Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3)
Write-Host "Assembly version: $version"

if (-not (Test-Path $dist)) {
    New-Item -ItemType Directory -Path $dist | Out-Null
}

$nupkgName = "PAMonitor.XrmToolBox.$version.nupkg"
$nupkgPath = Join-Path $dist $nupkgName

$nuget = Get-Command nuget -ErrorAction SilentlyContinue
if ($nuget) {
    Push-Location (Split-Path $nuspec -Parent)
    try {
        nuget pack $nuspec -OutputDirectory $dist -Properties "version=$version"
        if ($LASTEXITCODE -ne 0) {
            throw "nuget pack failed"
        }
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "nuget.exe not found; trying dotnet pack with nuspec..."
    Push-Location (Split-Path $nuspec -Parent)
    try {
        dotnet pack $nuspec /p:NuspecFile=$nuspec -c Release -o $dist --force
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet pack failed"
        }
    }
    finally {
        Pop-Location
    }
}

$created = Get-ChildItem $dist -Filter "PAMonitor.XrmToolBox.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $created) {
    throw "No .nupkg produced in $dist"
}

Write-Host ""
Write-Host "Package created: $($created.FullName)"
Write-Host ""
Write-Host "Verify with NuGet Package Explorer, then publish:"
Write-Host "  dotnet nuget push `"$($created.FullName)`" --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
