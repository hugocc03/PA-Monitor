$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$project = Join-Path $root "src\PAMonitor.XrmToolBox\PAMonitor.XrmToolBox.csproj"
$nuspec = Join-Path $root "src\PAMonitor.XrmToolBox\PAMonitor.XrmToolBox.nuspec"
$dist = Join-Path $root "dist"
$iconScript = Join-Path $root "tools\GeneratePluginIcons.ps1"
$nugetLocal = Join-Path $root "tools\nuget.exe"

if (-not (Test-Path $iconScript)) {
    throw "Icon generator not found: $iconScript"
}

# nuget.exe from PATH is often too old for <icon> / <readme>; prefer a modern local copy.
function Ensure-ModernNuget {
    $minMajor = 6
    $existing = Get-Command nuget -ErrorAction SilentlyContinue
    if ($existing) {
        $verText = (& nuget help 2>&1 | Select-Object -First 1) -replace '.*Version:\s*', ''
        $parts = $verText -split '\.'
        if ($parts.Count -ge 1 -and [int]$parts[0] -ge $minMajor) {
            return $existing.Source
        }
        Write-Host "PATH nuget.exe is $verText (need >= $minMajor). Using tools\nuget.exe instead."
    }

    if (-not (Test-Path $nugetLocal)) {
        Write-Host "Downloading nuget.exe (latest)..."
        $toolsDir = Split-Path $nugetLocal -Parent
        if (-not (Test-Path $toolsDir)) {
            New-Item -ItemType Directory -Path $toolsDir | Out-Null
        }
        Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetLocal
    }

    return $nugetLocal
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

$icon = Join-Path $root "assets\icon-128.png"
$readme = Join-Path $root "README.md"
if (-not (Test-Path $icon)) {
    throw "Package icon not found: $icon"
}
if (-not (Test-Path $readme)) {
    throw "README.md not found: $readme"
}

$version = [System.Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3)
Write-Host "Assembly version: $version"

if (-not (Test-Path $dist)) {
    New-Item -ItemType Directory -Path $dist | Out-Null
}

$nugetExe = Ensure-ModernNuget
Write-Host "Packing with: $nugetExe"

Push-Location (Split-Path $nuspec -Parent)
try {
    & $nugetExe pack $nuspec -OutputDirectory $dist -Properties "version=$version" -NonInteractive
    if ($LASTEXITCODE -ne 0) {
        throw "nuget pack failed"
    }
}
finally {
    Pop-Location
}

$created = Get-ChildItem $dist -Filter "PAMonitor.XrmToolBox.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $created) {
    throw "No .nupkg produced in $dist"
}

Write-Host ""
Write-Host "Package created: $($created.FullName)"
Write-Host ""
Write-Host "Publish:"
Write-Host "  dotnet nuget push `"$($created.FullName)`" --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
