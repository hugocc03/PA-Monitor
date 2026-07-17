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
    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "XrmToolBox\XrmToolBox.exe"),
        (Join-Path $env:APPDATA "MscrmTools\XrmToolBox\XrmToolBox.exe"),
        (Join-Path $env:USERPROFILE "Documents\XrmToolbox\XrmToolBox.exe")
    )

    foreach ($path in $candidates) {
        if (Test-Path $path) { return $path }
    }

    try {
        $dotnet = Get-DotNetExe
        $locals = & $dotnet nuget locals global-packages -l 2>$null
        if ($locals -match 'global-packages:\s*(.+)') {
            $nugetRoot = $Matches[1].Trim()
            $fromNuget = Get-ChildItem (Join-Path $nugetRoot "xrmtoolboxpackage") -Filter "XrmToolBox.exe" -Recurse -ErrorAction SilentlyContinue |
                Sort-Object FullName -Descending |
                Select-Object -First 1
            if ($fromNuget) { return $fromNuget.FullName }
        }
    }
    catch {
        # Ignore and continue with shortcut search.
    }

    $shortcut = Get-ChildItem "$env:APPDATA\Microsoft\Windows\Start Menu" -Filter "XrmToolBox*.lnk" -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($shortcut) {
        $shell = New-Object -ComObject WScript.Shell
        $target = $shell.CreateShortcut($shortcut.FullName).TargetPath
        if ($target -and (Test-Path $target)) { return $target }
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

$xrmToolBoxExe = Find-XrmToolBoxExe
if ($xrmToolBoxExe) {
    Start-Process $xrmToolBoxExe
    Write-Host "XrmToolBox opened: $xrmToolBoxExe"
} else {
    Write-Host "XrmToolBox.exe not found automatically. Open XrmToolBox manually to load PA Run Monitor."
}
