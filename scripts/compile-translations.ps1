<#
.SYNOPSIS
    Validates and compiles every Luna Player translation catalogue.

.DESCRIPTION
    Checks that translated named placeholders match their source messages, then compiles each
    locale\<language>\LC_MESSAGES\LunaPlayer.po file to LunaPlayer.mo with GNU msgfmt.

    Needs msgfmt on PATH or in one of the usual Windows gettext locations.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$localeRoot = Join-Path $root 'locale'

function Get-Msgfmt {
    $command = Get-Command 'msgfmt' -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($command) { return $command.Source }

    foreach ($directory in @(
        "${env:ProgramFiles(x86)}\GnuWin32\bin",
        "${env:ProgramFiles}\GnuWin32\bin",
        "${env:ProgramFiles}\gettext-iconv\bin")) {
        $candidate = Join-Path $directory 'msgfmt.exe'
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }
    throw 'msgfmt was not found. Install the GNU gettext tools and put their bin directory on PATH.'
}

function Get-QuotedText {
    param([Parameter(Mandatory)] [AllowEmptyString()] [string] $Text)

    $first = $Text.IndexOf('"')
    $last = $Text.LastIndexOf('"')
    if ($first -lt 0 -or $last -le $first) { return '' }
    return $Text.Substring($first + 1, $last - $first - 1).
        Replace('\"', '"').Replace('\n', "`n").Replace('\t', "`t").Replace('\\', '\')
}

function Get-CatalogEntry {
    param([Parameter(Mandatory)] [string] $Path)

    $entries = [System.Collections.Generic.List[hashtable]]::new()
    $current = $null
    $keyword = $null
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        $text = $line.Trim()
        if ($text.Length -eq 0) { $keyword = $null; continue }
        if ($text.StartsWith('#')) { continue }
        if ($text.StartsWith('"')) {
            if ($current -and $keyword) { $current[$keyword] += Get-QuotedText $text }
            continue
        }
        $space = $text.IndexOf(' ')
        if ($space -lt 0) { continue }
        $keyword = $text.Substring(0, $space)
        $value = Get-QuotedText $text.Substring($space + 1)
        if ($keyword -eq 'msgid') {
            $current = @{}
            $entries.Add($current)
        }
        if (-not $current) { continue }
        if (-not $current.ContainsKey($keyword)) { $current[$keyword] = '' }
        $current[$keyword] += $value
    }
    return $entries
}

function Get-PlaceholderNames {
    param([Parameter(Mandatory)] [AllowEmptyString()] [string] $Text)

    $names = [System.Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
    for ($index = 0; $index -lt $Text.Length; $index++) {
        if ($Text[$index] -ne '{') { continue }
        if ($index + 1 -lt $Text.Length -and $Text[$index + 1] -eq '{') { $index++; continue }
        $close = $Text.IndexOf('}', $index + 1)
        if ($close -lt 0) { break }
        $body = $Text.Substring($index + 1, $close - $index - 1)
        $colon = $body.IndexOf(':')
        if ($colon -ge 0) { $body = $body.Substring(0, $colon) }
        [void] $names.Add($body)
        $index = $close
    }
    return ($names -join ', ')
}

function Test-CatalogPlaceholders {
    param([Parameter(Mandatory)] [string] $Path, [Parameter(Mandatory)] [string] $Language)

    $problems = 0
    foreach ($entry in Get-CatalogEntry $Path) {
        $source = $entry['msgid']
        if ([string]::IsNullOrEmpty($source)) { continue }
        $expected = Get-PlaceholderNames $source
        foreach ($key in @($entry.Keys)) {
            if (-not $key.StartsWith('msgstr')) { continue }
            $translation = $entry[$key]
            if ([string]::IsNullOrEmpty($translation)) { continue }
            $actual = Get-PlaceholderNames $translation
            if ($actual -ceq $expected) { continue }
            $problems++
            Write-Warning ("{0}: placeholders differ for `"{1}`" - expected {2}, found {3}" -f `
                $Language, $source,
                ($(if ($expected) { $expected } else { 'none' })),
                ($(if ($actual) { $actual } else { 'none' })))
        }
    }
    return $problems
}

function Invoke-Msgfmt {
    param([Parameter(Mandatory)] [string] $Path, [Parameter(Mandatory)] [string[]] $Arguments)

    # msgfmt writes successful statistics to stderr. Windows PowerShell turns native stderr into error
    # records, so strict error handling must be relaxed only while that output is captured.
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $Path @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    $message = ($output | ForEach-Object { $_.ToString() }) -join ' '
    if ($exitCode -ne 0) {
        throw "$(Split-Path -Leaf $Path) failed with exit code $exitCode. $message"
    }
    return $message
}

$catalogues = @(Get-ChildItem -LiteralPath $localeRoot -Recurse -Filter 'LunaPlayer.po' -ErrorAction SilentlyContinue)
if ($catalogues.Count -eq 0) {
    Write-Host 'No translation catalogues were found.'
    return
}

$problems = 0
foreach ($catalogue in $catalogues) {
    $language = Split-Path -Leaf (Split-Path -Parent (Split-Path -Parent $catalogue.FullName))
    $problems += Test-CatalogPlaceholders $catalogue.FullName $language
}
if ($problems -gt 0) {
    throw "$problems translation(s) do not use the same placeholders as the source message."
}

$msgfmt = Get-Msgfmt
foreach ($catalogue in $catalogues) {
    $language = Split-Path -Leaf (Split-Path -Parent (Split-Path -Parent $catalogue.FullName))
    $binary = Join-Path $catalogue.DirectoryName 'LunaPlayer.mo'
    $statistics = Invoke-Msgfmt $msgfmt @('--check-format', '--statistics', "--output-file=$binary", $catalogue.FullName)
    Write-Host "$language`: $statistics"
}
