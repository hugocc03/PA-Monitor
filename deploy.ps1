$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "src\PAMonitor.XrmToolBox\PAMonitor.XrmToolBox.csproj"
$xrmToolBoxExe = "C:\Users\hugoc\Documents\XrmToolbox\XrmToolBox.exe"

Get-Process XrmToolBox -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

dotnet build $project -c Debug
if ($LASTEXITCODE -ne 0) {
    throw "Build failed"
}

if (-not (Test-Path $xrmToolBoxExe)) {
    throw "XrmToolBox not found: $xrmToolBoxExe"
}

Start-Process $xrmToolBoxExe
Write-Host "XrmToolBox opened."
