$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$base = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    $base = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
}
$wpf = Join-Path $base 'WPF'
if (-not (Test-Path $csc)) {
    throw 'Cannot find csc.exe from .NET Framework 4.x. Enable .NET Framework 4.8 first.'
}
if (-not (Test-Path (Join-Path $wpf 'PresentationFramework.dll'))) {
    throw 'Cannot find WPF runtime. Install .NET Framework 4.8.'
}

$outDir = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$out = Join-Path $outDir 'BlueFishPet.exe'

$refs = @(
    (Join-Path $wpf 'PresentationFramework.dll'),
    (Join-Path $wpf 'PresentationCore.dll'),
    (Join-Path $wpf 'WindowsBase.dll'),
    (Join-Path $base 'System.Xaml.dll'),
    (Join-Path $base 'System.Core.dll'),
    (Join-Path $base 'System.dll'),
    (Join-Path $base 'System.Drawing.dll'),
    (Join-Path $base 'System.Windows.Forms.dll'),
    (Join-Path $base 'System.Web.Extensions.dll'),
    (Join-Path $base 'System.Security.dll'),
    (Join-Path $base 'System.Xml.dll')
)

$cscArgs = @('/nologo', '/target:winexe', '/optimize+', '/platform:anycpu')
$cscArgs += '/win32icon:' + (Join-Path $root 'assets\fish.ico')
$cscArgs += '/win32manifest:' + (Join-Path $root 'app.manifest')
$cscArgs += '/out:' + $out
foreach ($key in @('daily','happy','confused','shy','eating','sad','ok','angry','sleep','cry','lie','shock')) {
    $cscArgs += '/resource:' + (Join-Path $root ('assets\expressions\' + $key + '.png')) + ',BlueFishPet.Assets.' + $key + '.png'
}
$cscArgs += '/resource:' + (Join-Path $root 'assets\sprites\fish_side.png') + ',BlueFishPet.Assets.side.png'
foreach ($r in $refs) { $cscArgs += '/r:' + $r }
Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' | Sort-Object Name | ForEach-Object { $cscArgs += $_.FullName }

Write-Host 'Building BlueFishPet...' -ForegroundColor Cyan
& $csc $cscArgs
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
$item = Get-Item $out
Write-Host ("Done: {0}  ({1:N0} KB)" -f $item.FullName, ($item.Length / 1KB)) -ForegroundColor Green
