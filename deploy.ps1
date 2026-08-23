param(
    [switch]$Launch,
    [string]$XrmToolBoxExe
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "src\PAMonitor.XrmToolBox\PAMonitor.XrmToolBox.csproj"

function Get-DotNetExe {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $default = Join-Path ${env:ProgramFiles} "dotnet\dotnet.exe"
    if (Test-Path $default) { return $default }

    throw "dotnet CLI not found. Install .NET SDK 8+ or add dotnet to PATH."
}

function Find-XrmToolBoxExe {
    if (-not [string]::IsNullOrWhiteSpace($XrmToolBoxExe) -and (Test-Path $XrmToolBoxExe)) {
        return $XrmToolBoxExe
    }

    if (-not [string]::IsNullOrWhiteSpace($env:XRMTOOLBOX_EXE) -and (Test-Path $env:XRMTOOLBOX_EXE)) {
        return $env:XRMTOOLBOX_EXE
    }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "XrmToolBox\XrmToolBox.exe"),
        (Join-Path $env:APPDATA "MscrmTools\XrmToolBox\XrmToolBox.exe"),
        (Join-Path $env:USERPROFILE "Documents\XrmToolbox\XrmToolBox.exe")
    )

    foreach ($path in $candidates) {
        if (Test-Path $path) { return $path }
    }

    return $null
}

Get-Process XrmToolBox -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

$dotnet = Get-DotNetExe
& $dotnet build $project -c Debug
if ($LASTEXITCODE -ne 0) {
    throw "Build failed"
}

$pluginsPath = Join-Path $env:APPDATA "MscrmTools\XrmToolBox\Plugins"
Write-Host "Plugin deployed to: $pluginsPath"

if ($Launch) {
    $exe = Find-XrmToolBoxExe
    if ($exe) {
        Start-Process $exe
        Write-Host "XrmToolBox opened: $exe"
    } else {
        Write-Host "XrmToolBox.exe not found. Set -XrmToolBoxExe or env XRMTOOLBOX_EXE, then use -Launch."
    }
} else {
    Write-Host "Build done. Reopen XrmToolBox yourself (or run with -Launch)."
}
