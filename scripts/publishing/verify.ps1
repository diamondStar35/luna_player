$ErrorActionPreference = 'Stop'
$metadata = Get-Content release-metadata.json -Raw | ConvertFrom-Json
$executable = Get-Item 'src/bin/Release/net10.0-windows10.0.19041.0/win-x64/LunaPlayer.exe'
$builtVersion = ([string]$executable.VersionInfo.ProductVersion).Split('+')[0]
if ($builtVersion -ne $metadata.version) {
  throw "Built application version '$builtVersion' does not match release version '$($metadata.version)'."
}
if (-not (Test-Path -LiteralPath 'src/bin/Release/net10.0-windows10.0.19041.0/win-x64/Updater.exe')) {
  throw "Updater.exe was not included in the published application."
}
if (-not (Test-Path -LiteralPath 'src/bin/Release/net10.0-windows10.0.19041.0/win-x64/docs/en/user-guide.html')) {
  throw "The rendered English user guide was not included in the published application."
}
if (Test-Path -LiteralPath 'src/bin/Release/net10.0-windows10.0.19041.0/win-x64/docs/en/user-guide.md') {
  throw "The Markdown user-guide source was included in the published application."
}

$localeRoot = (Resolve-Path 'locale').Path
$publishRoot = (Resolve-Path 'src/bin/Release/net10.0-windows10.0.19041.0/win-x64').Path
$catalogues = @(Get-ChildItem -LiteralPath $localeRoot -Recurse -Filter 'LunaPlayer.po')
if ($catalogues.Count -eq 0) {
  throw 'No translation catalogues were found.'
}
foreach ($catalogue in $catalogues) {
  $compiledCatalogue = [System.IO.Path]::ChangeExtension($catalogue.FullName, '.mo')
  $relativePath = [System.IO.Path]::GetRelativePath($localeRoot, $compiledCatalogue)
  $publishedCatalogue = Join-Path (Join-Path $publishRoot 'locale') $relativePath
  if (-not (Test-Path -LiteralPath $publishedCatalogue)) {
    throw "The compiled translation '$relativePath' was not included in the published application."
  }
}
