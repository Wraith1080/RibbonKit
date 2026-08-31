param(
    [string] $OutputPath = (Join-Path $PSScriptRoot 'Writer.ico')
)

Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath([float] $x, [float] $y, [float] $width, [float] $height, [float] $radius) {
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $radius * 2
    $path.AddArc($x, $y, $diameter, $diameter, 180, 90)
    $path.AddArc($x + $width - $diameter, $y, $diameter, $diameter, 270, 90)
    $path.AddArc($x + $width - $diameter, $y + $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($x, $y + $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-WriterIconFrame([int] $size) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $scale = $size / 256.0

        $tile = New-RoundedRectanglePath (8 * $scale) (8 * $scale) (240 * $scale) (240 * $scale) (48 * $scale)
        try {
            $tileBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
                [System.Drawing.PointF]::new(0, 8 * $scale),
                [System.Drawing.PointF]::new(0, 248 * $scale),
                [System.Drawing.ColorTranslator]::FromHtml('#3F94DF'),
                [System.Drawing.ColorTranslator]::FromHtml('#145AA6'))
            try { $graphics.FillPath($tileBrush, $tile) } finally { $tileBrush.Dispose() }
        }
        finally { $tile.Dispose() }

        $paper = [System.Drawing.Drawing2D.GraphicsPath]::new()
        try {
            $paper.AddPolygon([System.Drawing.PointF[]] @(
                [System.Drawing.PointF]::new(62 * $scale, 39 * $scale),
                [System.Drawing.PointF]::new(153 * $scale, 39 * $scale),
                [System.Drawing.PointF]::new(196 * $scale, 82 * $scale),
                [System.Drawing.PointF]::new(196 * $scale, 217 * $scale),
                [System.Drawing.PointF]::new(62 * $scale, 217 * $scale)))
            $graphics.FillPath([System.Drawing.Brushes]::White, $paper)
        }
        finally { $paper.Dispose() }

        $fold = [System.Drawing.Drawing2D.GraphicsPath]::new()
        try {
            $fold.AddPolygon([System.Drawing.PointF[]] @(
                [System.Drawing.PointF]::new(153 * $scale, 39 * $scale),
                [System.Drawing.PointF]::new(153 * $scale, 82 * $scale),
                [System.Drawing.PointF]::new(196 * $scale, 82 * $scale)))
            $foldBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#D6E9FA'))
            try { $graphics.FillPath($foldBrush, $fold) } finally { $foldBrush.Dispose() }
        }
        finally { $fold.Dispose() }

        $mark = [System.Drawing.Drawing2D.GraphicsPath]::new()
        try {
            $mark.AddPolygon([System.Drawing.PointF[]] @(
                [System.Drawing.PointF]::new(79 * $scale, 102 * $scale),
                [System.Drawing.PointF]::new(107 * $scale, 102 * $scale),
                [System.Drawing.PointF]::new(124 * $scale, 167 * $scale),
                [System.Drawing.PointF]::new(141 * $scale, 125 * $scale),
                [System.Drawing.PointF]::new(158 * $scale, 167 * $scale),
                [System.Drawing.PointF]::new(175 * $scale, 102 * $scale),
                [System.Drawing.PointF]::new(203 * $scale, 102 * $scale),
                [System.Drawing.PointF]::new(175 * $scale, 202 * $scale),
                [System.Drawing.PointF]::new(151 * $scale, 202 * $scale),
                [System.Drawing.PointF]::new(141 * $scale, 171 * $scale),
                [System.Drawing.PointF]::new(131 * $scale, 202 * $scale),
                [System.Drawing.PointF]::new(107 * $scale, 202 * $scale)))
            $markBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#1F6EB8'))
            try { $graphics.FillPath($markBrush, $mark) } finally { $markBrush.Dispose() }
        }
        finally { $mark.Dispose() }

        $stream = [System.IO.MemoryStream]::new()
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return $stream.ToArray()
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$frames = [System.Collections.Generic.List[byte[]]]::new()
foreach ($size in $sizes) {
    $frames.Add([byte[]] (New-WriterIconFrame $size))
}
$file = [System.IO.File]::Create($OutputPath)
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16] 0)
    $writer.Write([uint16] 1)
    $writer.Write([uint16] $frames.Count)
    $offset = 6 + (16 * $frames.Count)
    for ($index = 0; $index -lt $frames.Count; $index++) {
        $size = $sizes[$index]
        $writer.Write([byte] $(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte] $(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte] 0)
        $writer.Write([byte] 0)
        $writer.Write([uint16] 1)
        $writer.Write([uint16] 32)
        $writer.Write([uint32] $frames[$index].Length)
        $writer.Write([uint32] $offset)
        $offset += $frames[$index].Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]] $frame) }
}
finally {
    $writer.Dispose()
    $file.Dispose()
}

Write-Host "Generated $OutputPath with $($frames.Count) PNG frames."
