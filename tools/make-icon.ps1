# Erzeugt das MouseMeter-Anwendungssymbol (Multi-Size .ico mit PNG-Eintraegen).
# Aufruf:  powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
Add-Type -AssemblyName System.Drawing

$outPath = Join-Path $PSScriptRoot '..\src\MouseMeter\Assets\MouseMeter.ico'
$outPath = [System.IO.Path]::GetFullPath($outPath)
$sizes   = @(16, 24, 32, 48, 64, 128, 256)

function New-RoundedPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$rTop, [float]$rBottom) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $p.AddArc($x, $y, 2 * $rTop, 2 * $rTop, 180, 90)
    $p.AddArc($x + $w - 2 * $rTop, $y, 2 * $rTop, 2 * $rTop, 270, 90)
    $p.AddArc($x + $w - 2 * $rBottom, $y + $h - 2 * $rBottom, 2 * $rBottom, 2 * $rBottom, 0, 90)
    $p.AddArc($x, $y + $h - 2 * $rBottom, 2 * $rBottom, 2 * $rBottom, 90, 90)
    $p.CloseFigure()
    return $p
}

function New-LogoBitmap([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $f = [float]$s

    # Maus-Koerper
    $bx = 0.255 * $f; $by = 0.075 * $f; $bw = 0.49 * $f; $bh = 0.85 * $f
    $body = New-RoundedPath $bx $by $bw $bh (0.245 * $f) (0.235 * $f)

    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF($bx, $by)),
        (New-Object System.Drawing.PointF(($bx + $bw), ($by + $bh))),
        [System.Drawing.Color]::FromArgb(255, 96, 165, 250),
        [System.Drawing.Color]::FromArgb(255, 34, 211, 238))
    $g.FillPath($grad, $body)

    # Scrollrad
    $ww = [Math]::Max(1.0, 0.075 * $f)
    $wh = [Math]::Max(2.0, 0.165 * $f)
    $wx = ($f - $ww) / 2.0
    $wy = 0.215 * $f
    $wheel = New-RoundedPath $wx $wy $ww $wh ($ww / 2.0) ($ww / 2.0)
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
    $g.FillPath($white, $wheel)

    # Bewegungsspur (erst ab mittlerer Groesse lesbar)
    if ($s -ge 32) {
        $penW = [Math]::Max(1.0, 0.055 * $f)
        $pen  = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(190, 34, 211, 238), $penW)
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawArc($pen, (0.045 * $f), (0.20 * $f), (0.62 * $f), (0.60 * $f), 120, 120)
        $pen.Dispose()
    }

    $white.Dispose(); $grad.Dispose(); $body.Dispose(); $wheel.Dispose(); $g.Dispose()
    return $bmp
}

# PNG-Daten je Groesse sammeln
$images = @()
foreach ($s in $sizes) {
    $bmp = New-LogoBitmap $s
    $ms  = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += [pscustomobject]@{ Size = $s; Data = $ms.ToArray() }
    $ms.Dispose(); $bmp.Dispose()
}

# ICO-Container schreiben (ICONDIR + ICONDIRENTRY je Bild + PNG-Daten)
$fs = [System.IO.File]::Create($outPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($img in $images) {
    $dim = if ($img.Size -ge 256) { 0 } else { $img.Size }
    $bw.Write([Byte]$dim); $bw.Write([Byte]$dim)
    $bw.Write([Byte]0); $bw.Write([Byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$img.Data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $img.Data.Length
}
foreach ($img in $images) { $bw.Write($img.Data) }
$bw.Flush(); $bw.Close(); $fs.Dispose()

Write-Output "Symbol erzeugt: $outPath ($((Get-Item $outPath).Length) Bytes)"
