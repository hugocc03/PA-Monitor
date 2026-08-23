# Generates plugin icons (32, 80, 128) for PA Run Monitor.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

function Draw-PluginIcon([System.Drawing.Graphics]$g, [int]$size) {
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::FromArgb(245, 248, 252))

    $scale = $size / 32.0

    # Monitor frame
    $frame = New-Object System.Drawing.RectangleF (2 * $scale), (3 * $scale), (28 * $scale), (20 * $scale)
    $framePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(50, 90, 160)), (1.6 * $scale)
    $frameFill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(220, 235, 252))
    $g.FillRectangle($frameFill, $frame)
    $g.DrawRectangle($framePen, $frame.X, $frame.Y, $frame.Width, $frame.Height)

    # Flow nodes
    $nodeBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(20, 130, 120))
    $nodePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(20, 130, 120)), (1.4 * $scale)
    $r = 3.5 * $scale
    $g.FillEllipse($nodeBrush, 6 * $scale, 8 * $scale, $r, $r)
    $g.FillEllipse($nodeBrush, 6 * $scale, 16 * $scale, $r, $r)
    $g.FillEllipse($nodeBrush, 18 * $scale, 12 * $scale, $r, $r)
    $g.DrawLine($nodePen, 9 * $scale, 10 * $scale, 18 * $scale, 13 * $scale)
    $g.DrawLine($nodePen, 9 * $scale, 18 * $scale, 18 * $scale, 15 * $scale)

    # Play triangle (run)
    $playBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(50, 140, 70))
    $play = @(
        (New-Object System.Drawing.PointF ([double](22 * $scale)), ([double](9 * $scale))),
        (New-Object System.Drawing.PointF ([double](22 * $scale)), ([double](19 * $scale))),
        (New-Object System.Drawing.PointF ([double](28 * $scale)), ([double](14 * $scale)))
    )
    $g.FillPolygon($playBrush, $play)

    # Stand
    $standPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(50, 90, 160)), (1.8 * $scale)
    $g.DrawLine($standPen, 16 * $scale, 23 * $scale, 16 * $scale, 27 * $scale)
    $g.DrawLine($standPen, 10 * $scale, 27 * $scale, 22 * $scale, 27 * $scale)

    $framePen.Dispose(); $frameFill.Dispose(); $nodeBrush.Dispose(); $nodePen.Dispose(); $playBrush.Dispose(); $standPen.Dispose()
}

function Save-Icon([int]$size, [string]$path) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try { Draw-PluginIcon $g $size } finally { $g.Dispose() }
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$root = if (Test-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "src")) {
    Split-Path $PSScriptRoot -Parent
} else {
    Split-Path $PSScriptRoot -Parent
}
$assets = Join-Path $root "assets"
Save-Icon 32 (Join-Path $assets "icon-32.png")
Save-Icon 80 (Join-Path $assets "icon-80.png")
Save-Icon 128 (Join-Path $assets "icon-128.png")

function Get-Base64([string]$path) {
    [Convert]::ToBase64String([IO.File]::ReadAllBytes($path))
}

Write-Host "icon-32 base64 length:" (Get-Base64 (Join-Path $assets "icon-32.png")).Length
Write-Host "icon-80 base64 length:" (Get-Base64 (Join-Path $assets "icon-80.png")).Length
Write-Host "Icons saved to $assets"
