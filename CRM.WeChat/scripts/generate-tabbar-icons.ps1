# 生成 TabBar 占位图标（81x81 PNG）
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$outDir = Join-Path $PSScriptRoot '..\src\static\tabbar'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Save-Icon([string]$name, [int]$r, [int]$g, [int]$b) {
  $size = 81
  $bmp = New-Object System.Drawing.Bitmap $size, $size
  $gfx = [System.Drawing.Graphics]::FromImage($bmp)
  $gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $gfx.Clear([System.Drawing.Color]::Transparent)

  $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb($r, $g, $b))
  $rect = New-Object System.Drawing.Rectangle 14, 14, 53, 53
  $gfx.FillEllipse($brush, $rect)
  $brush.Dispose()
  $gfx.Dispose()

  $path = Join-Path $outDir $name
  $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
  $bmp.Dispose()
  Write-Host "Created $path"
}

$pairs = @(
  @{ Normal = 'home.png'; Active = 'home-active.png' },
  @{ Normal = 'customer.png'; Active = 'customer-active.png' },
  @{ Normal = 'order.png'; Active = 'order-active.png' },
  @{ Normal = 'inventory.png'; Active = 'inventory-active.png' },
  @{ Normal = 'mine.png'; Active = 'mine-active.png' }
)

foreach ($p in $pairs) {
  Save-Icon $p.Normal 153 153 153
  Save-Icon $p.Active 22 119 255
}
