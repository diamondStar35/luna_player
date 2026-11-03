param([ValidateSet('stable', 'nightly')] [string] $Channel)
$ErrorActionPreference = 'Stop'
$info = Get-Content info.json -Raw | ConvertFrom-Json
$version = ([string]$info.version).Trim()
$tag = if ($Channel -eq 'nightly') { 'nightly' } else { "v$version" }
$needed = $true
if ($Channel -eq 'stable') {
    $existing = @(git ls-remote --tags origin "refs/tags/$tag")
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect release tags.' }
    $needed = $existing.Count -eq 0
}
@{
    version = $version; tag = $tag
    archive = "Luna_Player-v-$version.zip"; installer = "Luna_Player-v-$version.exe"
    changes = @($info.changes); sha = (git rev-parse HEAD).Trim()
    built_at = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm UTC')
} | ConvertTo-Json -Depth 4 | Set-Content release-metadata.json -Encoding utf8
"publish=$($needed.ToString().ToLowerInvariant())" >> $env:GITHUB_OUTPUT
