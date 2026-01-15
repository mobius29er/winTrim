# WinTrim Icon Generator
# Run this script to generate the application icon

# Create a simple icon using .NET
Add-Type -AssemblyName System.Drawing

$iconSize = 256
$bitmap = New-Object System.Drawing.Bitmap($iconSize, $iconSize)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# Background - dark gradient
$backgroundBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(30, 30, 30))
$graphics.FillRectangle($backgroundBrush, 0, 0, $iconSize, $iconSize)

# Draw scissors/trim icon shape
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(16, 185, 129), 20)  # Green color
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

# Scissors blade 1
$graphics.DrawLine($pen, 60, 80, 196, 176)
# Scissors blade 2  
$graphics.DrawLine($pen, 60, 176, 196, 80)

# Center pivot circle
$centerBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(16, 185, 129))
$graphics.FillEllipse($centerBrush, 108, 108, 40, 40)

# Disk/drive shape (circle outline)
$diskPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(59, 130, 246), 8)  # Blue
$graphics.DrawEllipse($diskPen, 40, 40, 176, 176)

$graphics.Dispose()

# Save as PNG first
$pngPath = Join-Path $PSScriptRoot "wintrim.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Convert to ICO
$icoPath = Join-Path $PSScriptRoot "wintrim.ico"

# Create icon from bitmap
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$fileStream = [System.IO.File]::Create($icoPath)
$icon.Save($fileStream)
$fileStream.Close()

$bitmap.Dispose()

Write-Host "Icon created at: $icoPath" -ForegroundColor Green
