$ErrorActionPreference = 'Stop'
$metadata = Get-Content release-metadata.json -Raw | ConvertFrom-Json
$source = 'dist/LunaPlayerSetup.exe'
if (-not (Test-Path -LiteralPath $source)) {
  throw "Expected installer '$source' was not created."
}
Copy-Item -LiteralPath $source -Destination (Join-Path 'release-assets' $metadata.installer)

$assetDirectory = Join-Path $env:GITHUB_WORKSPACE 'release-assets'
$lines = @($metadata.archive, $metadata.installer) | ForEach-Object {
  $path = Join-Path $assetDirectory $_
  if (-not (Test-Path -LiteralPath $path)) {
    throw "Cannot checksum missing release asset '$path'."
  }
  $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
  "$hash  $_"
}
$lines | Set-Content -LiteralPath (Join-Path $assetDirectory 'SHA256SUMS.txt') -Encoding ascii
