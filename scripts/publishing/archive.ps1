$ErrorActionPreference = 'Stop'
$metadata = Get-Content release-metadata.json -Raw | ConvertFrom-Json
$publishDirectory = Resolve-Path 'src/bin/Release/net10.0-windows10.0.19041.0/win-x64'
$assetDirectory = Join-Path $env:GITHUB_WORKSPACE 'release-assets'
$stageDirectory = Join-Path $env:RUNNER_TEMP "LunaPlayer-$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT"
New-Item -ItemType Directory -Path $assetDirectory, $stageDirectory -Force | Out-Null

& robocopy $publishDirectory $stageDirectory /E /XF *.pdb *.xml
if ($LASTEXITCODE -ge 8) {
  throw "Could not stage the published application; robocopy exited with $LASTEXITCODE."
}
$global:LASTEXITCODE = 0

$archivePath = Join-Path $assetDirectory $metadata.archive
Compress-Archive -Path "$stageDirectory\*" -DestinationPath $archivePath -CompressionLevel Optimal
