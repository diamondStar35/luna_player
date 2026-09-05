$ErrorActionPreference = 'Stop'

function Fail([string]$message) {
    Write-Host $message -ForegroundColor Red
    exit 1
}

$repoRoot = (& git rev-parse --show-toplevel).Trim()
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    Fail 'Commit blocked: could not resolve the repository root.'
}

function Read-StagedFile([string]$relativePath) {
    $content = & git -C $repoRoot show ":$relativePath" 2>$null
    if ($LASTEXITCODE -ne 0) {
        Fail "Commit blocked: '$relativePath' is missing from the commit."
    }
    return ($content -join "`n")
}

$infoText = Read-StagedFile 'info.json'
$projectText = Read-StagedFile 'src/LunaPlayer.csproj'

try {
    $info = $infoText | ConvertFrom-Json -ErrorAction Stop
} catch {
    Fail "Commit blocked: info.json is invalid JSON. Details: $($_.Exception.Message)"
}

try {
    [xml]$project = $projectText
} catch {
    Fail "Commit blocked: src/LunaPlayer.csproj is invalid XML. Details: $($_.Exception.Message)"
}

$infoVersion = [string]$info.version
if ([string]::IsNullOrWhiteSpace($infoVersion)) {
    Fail "Commit blocked: info.json is missing a valid 'version' value."
}
$infoVersion = $infoVersion.Trim()

$versionPattern = '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$'
if ($infoVersion -notmatch $versionPattern) {
    Fail "Commit blocked: info.json version '$infoVersion' is not a valid release version."
}

$versionNodes = @($project.SelectNodes('/Project/PropertyGroup/Version'))
if ($versionNodes.Count -ne 1) {
    Fail "Commit blocked: expected exactly one <Version> in src/LunaPlayer.csproj."
}
$projectVersion = $versionNodes[0].InnerText.Trim()

if ($projectVersion -ne $infoVersion) {
    Fail @"
Commit blocked: release version mismatch.
- src/LunaPlayer.csproj: $projectVersion
- info.json: $infoVersion

Update both files to the same version and commit again.
"@
}

if ($null -eq $info.changes -or $info.changes -isnot [System.Array]) {
    Fail "Commit blocked: info.json 'changes' must be a JSON list."
}

if ($info.changes.Count -eq 0) {
    Fail "Commit blocked: info.json 'changes' must contain at least one change."
}
foreach ($change in $info.changes) {
    if ($change -isnot [string] -or [string]::IsNullOrWhiteSpace($change)) {
        Fail "Commit blocked: every item in info.json 'changes' must be a non-empty string."
    }
}

exit 0
