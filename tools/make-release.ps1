<#
    Legt einen vollstaendigen Projektstand als ZIP unter releases/ ab.

    Aufruf:  powershell -ExecutionPolicy Bypass -File tools\make-release.ps1

    Die Version wird aus src/MouseMeter/MouseMeter.csproj gelesen.
    Ein bereits vorhandenes Archiv wird niemals ueberschrieben - jede Version
    bleibt als Ruecksprungpunkt erhalten.
#>
[CmdletBinding()]
param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$root    = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$csproj  = Join-Path $root 'src\MouseMeter\MouseMeter.csproj'

if (-not $Version) {
    [xml]$doc = Get-Content -LiteralPath $csproj
    $Version  = ($doc.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
}
if (-not $Version) { throw "Version konnte nicht aus $csproj gelesen werden." }

$releases = Join-Path $root 'releases'
New-Item -ItemType Directory -Force -Path $releases | Out-Null

$zipPath = Join-Path $releases ("MouseMeter_v{0}.zip" -f $Version)
if (Test-Path -LiteralPath $zipPath) {
    throw "Archiv existiert bereits und wird nicht ueberschrieben: $zipPath"
}

# Inhalte, die in jeden Projektstand gehoeren
$include = @('src', 'tools', 'docs', 'README.md', 'CHANGELOG.md', '.gitignore')
# Ordner, die nie mitgesichert werden
$excludeDirs = @('bin', 'obj', 'publish', '.vs', '.idea', '.git')

$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("MouseMeter_v{0}_{1}" -f $Version, [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Force -Path $staging | Out-Null

try {
    foreach ($item in $include) {
        $source = Join-Path $root $item
        if (-not (Test-Path -LiteralPath $source)) { continue }

        if (Test-Path -LiteralPath $source -PathType Container) {
            $target = Join-Path $staging $item
            Copy-Item -LiteralPath $source -Destination $target -Recurse -Force
            foreach ($dir in $excludeDirs) {
                Get-ChildItem -LiteralPath $target -Directory -Recurse -Force -Filter $dir -ErrorAction SilentlyContinue |
                    Sort-Object { $_.FullName.Length } -Descending |
                    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
            }
        }
        else {
            Copy-Item -LiteralPath $source -Destination (Join-Path $staging $item) -Force
        }
    }

    Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zipPath -CompressionLevel Optimal
}
finally {
    Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
}

$size = [Math]::Round((Get-Item -LiteralPath $zipPath).Length / 1KB, 1)
Write-Output "Projektstand gesichert: $zipPath ($size KB)"
