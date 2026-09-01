<#
.SYNOPSIS
    Rebuilds the translation template from the source and brings the catalogues up to date.

.DESCRIPTION
    Three steps, each of which can be run on its own:

      1. Extract every string wrapped in Tr, TrFormat, TrPlural or TrPluralFormat out of src\ into
         locale\LunaPlayer.pot, carrying the "Translators:" comment above each call with it.
      2. Merge that template into every locale\<language>\LC_MESSAGES\LunaPlayer.po, so a translator
         sees new strings as untranslated and changed ones as fuzzy instead of losing their work.
      3. Compile each .po to the .mo the player loads, and check that the placeholder names in every
         translation match the ones in the string it translates.

    Needs the GNU gettext tools. They are found on PATH, or in the usual GnuWin32 location.

.PARAMETER Compile
    Only compile and check the existing catalogues; do not touch the template or the .po files.

.PARAMETER Language
    Start a new catalogue for this language code (for example "ar" or "pt_BR") if it does not exist
    yet, then carry on as usual.
#>
[CmdletBinding()]
param(
    [switch] $Compile,
    [string] $Language
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'src'
$localeRoot = Join-Path $root 'locale'
$template = Join-Path $localeRoot 'LunaPlayer.pot'

function Get-GettextTool {
    param([Parameter(Mandatory)] [string] $Name)

    $command = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($command) { return $command.Source }
    foreach ($directory in @(
        "${env:ProgramFiles(x86)}\GnuWin32\bin",
        "${env:ProgramFiles}\GnuWin32\bin",
        "${env:ProgramFiles}\gettext-iconv\bin")) {
        $candidate = Join-Path $directory "$Name.exe"
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }
    throw "$Name was not found. Install the GNU gettext tools and put their bin directory on PATH."
}

function Invoke-Tool {
    param([Parameter(Mandatory)] [string] $Path, [Parameter(Mandatory)] [string[]] $Arguments)

    & $Path @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$(Split-Path -Leaf $Path) failed with exit code $LASTEXITCODE."
    }
}

function Get-PlaceholderName {
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
    # Joined rather than returned as a set: an empty collection does not survive PowerShell's
    # pipeline, and the names are only ever compared as a whole and shown to the reader.
    return ($names -join ', ')
}

# The .po syntax this needs is small: a run of lines per entry, each keyword followed by one or more
# quoted pieces that concatenate. Anything else in the file is left alone.
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
            $current = @{ Line = 0 }
            $entries.Add($current)
        }
        if (-not $current) { continue }
        if (-not $current.ContainsKey($keyword)) { $current[$keyword] = '' }
        $current[$keyword] += $value
    }
    return $entries
}

function Get-QuotedText {
    param([Parameter(Mandatory)] [AllowEmptyString()] [string] $Text)

    $first = $Text.IndexOf('"')
    $last = $Text.LastIndexOf('"')
    if ($first -lt 0 -or $last -le $first) { return '' }
    return $Text.Substring($first + 1, $last - $first - 1).
        Replace('\"', '"').Replace('\n', "`n").Replace('\t', "`t").Replace('\\', '\')
}

# msgfmt checks the C and C# format strings it knows about, but the player's placeholders are named
# and it has no idea about those. A translation that renames one loses its whole message at run time,
# so the names are compared here instead.
function Test-Placeholder {
    param([Parameter(Mandatory)] [string] $Path, [Parameter(Mandatory)] [string] $Language)

    $problems = 0
    foreach ($entry in Get-CatalogEntry $Path) {
        $source = $entry['msgid']
        if ([string]::IsNullOrEmpty($source)) { continue }
        $expected = Get-PlaceholderName $source
        foreach ($key in @($entry.Keys)) {
            if (-not $key.StartsWith('msgstr')) { continue }
            $translation = $entry[$key]
            if ([string]::IsNullOrEmpty($translation)) { continue }
            $actual = Get-PlaceholderName $translation
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

if (-not $Compile) {
    $xgettext = Get-GettextTool 'xgettext'
    New-Item -ItemType Directory -Force -Path $localeRoot | Out-Null
    $list = New-TemporaryFile
    try {
        # Paths go in relative to src so the comments in the template point at something a translator
        # can find, which means the file list has to be relative too and xgettext has to run there.
        Push-Location $source
        try {
            Get-ChildItem -Recurse -Filter '*.cs' |
                Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
                ForEach-Object { [System.IO.Path]::GetRelativePath($source, $_.FullName).Replace('\', '/') } |
                Sort-Object |
                Set-Content -LiteralPath $list -Encoding UTF8
            # No --package-name here: the gettext build commonly installed on Windows is old enough to
            # reject it. The header is edited by hand instead.
            Invoke-Tool $xgettext @(
                '--language=C#'
                '--from-code=UTF-8'
                '--keyword=Tr:1'
                '--keyword=TrFormat:1'
                '--keyword=TrPlural:1,2'
                '--keyword=TrPluralFormat:1,2'
                '--add-comments=Translators'
                '--sort-by-file'
                '--copyright-holder=Luna Player contributors'
                '--msgid-bugs-address=https://github.com/diamondStar35/luna_player/issues'
                "--output=$template"
                "--files-from=$list")
        }
        finally { Pop-Location }
    }
    finally { Remove-Item -LiteralPath $list -Force -ErrorAction SilentlyContinue }

    # xgettext leaves its placeholders in the header. Fill in the ones that are the same for every
    # extraction so a new catalogue starts out valid, and say UTF-8 outright: without it msginit and
    # msgmerge ask what the charset is.
    $header = [System.IO.File]::ReadAllText($template)
    $header = $header.
        Replace('SOME DESCRIPTIVE TITLE.', 'Luna Player translation template.').
        Replace('Copyright (C) YEAR', 'Copyright (C) 2026').
        Replace('the PACKAGE package', 'Luna Player itself').
        Replace('Project-Id-Version: PACKAGE VERSION', 'Project-Id-Version: Luna Player').
        Replace('charset=CHARSET', 'charset=UTF-8')
    [System.IO.File]::WriteAllText($template, $header, (New-Object System.Text.UTF8Encoding $false))

    $strings = (Select-String -LiteralPath $template -Pattern '^msgid "' -AllMatches).Count - 1
    Write-Host "Extracted $strings strings into locale\LunaPlayer.pot."
}

if ($Language) {
    $directory = Join-Path $localeRoot (Join-Path $Language 'LC_MESSAGES')
    $catalog = Join-Path $directory 'LunaPlayer.po'
    if (Test-Path -LiteralPath $catalog) {
        Write-Host "$Language already has a catalogue; leaving it to the merge below."
    }
    else {
        # A new catalogue is the template with its header filled in. msginit would normally do this, but
        # the GnuWin32 build of it cannot run its own helper programs, so it is done here instead.
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
        # Only the languages a catalogue exists for need to be right here; anything else gets the rule
        # most European languages use and the translator corrects the header if their language differs.
        $plurals = @{
            'ar' = 'nplurals=6; plural=(n==0 ? 0 : n==1 ? 1 : n==2 ? 2 : n%100>=3 && n%100<=10 ? 3 : n%100>=11 ? 4 : 5);'
            'ja' = 'nplurals=1; plural=0;'
            'zh' = 'nplurals=1; plural=0;'
            'ru' = 'nplurals=3; plural=(n%10==1 && n%100!=11 ? 0 : n%10>=2 && n%10<=4 && (n%100<12 || n%100>14) ? 1 : 2);'
            'pl' = 'nplurals=3; plural=(n==1 ? 0 : n%10>=2 && n%10<=4 && (n%100<12 || n%100>14) ? 1 : 2);'
            'fr' = 'nplurals=2; plural=(n > 1);'
            'pt' = 'nplurals=2; plural=(n > 1);'
        }
        $escape = [string] [char] 92
        $base = $Language.Split(@('_', '-'))[0].ToLowerInvariant()
        $rule = if ($plurals.ContainsKey($base)) { $plurals[$base] } else { 'nplurals=2; plural=(n != 1);' }
        $text = [System.IO.File]::ReadAllText($template)
        # The template is marked fuzzy so nothing mistakes it for a translation; a catalogue is not.
        $text = $text -replace '(?m)^#, fuzzy\r?\n', ''
        $text = $text.
            Replace('PO-Revision-Date: YEAR-MO-DA HO:MI+ZONE', "PO-Revision-Date: $(Get-Date -Format 'yyyy-MM-dd HH:mmzzz')").
            Replace('Language-Team: LANGUAGE <LL@li.org>', 'Language-Team: ').
            Replace('Plural-Forms: nplurals=INTEGER; plural=EXPRESSION;', "Plural-Forms: $rule")
        # gettext leaves the Language field out of a template, but msgfmt and the translation editors
        # all expect a catalogue to name its language.
        $field = '"Language: ' + $Language + $escape + 'n"'
        $text = $text -replace '(?m)^"MIME-Version', ($field + "`r`n`"MIME-Version")
        [System.IO.File]::WriteAllText($catalog, $text, (New-Object System.Text.UTF8Encoding $false))
        Write-Host "Started locale\$Language\LC_MESSAGES\LunaPlayer.po."
    }
}

$catalogs = @(Get-ChildItem -LiteralPath $localeRoot -Recurse -Filter 'LunaPlayer.po' -ErrorAction SilentlyContinue)
if ($catalogs.Count -eq 0) {
    Write-Host 'No catalogues to update yet. Add one with -Language <code>.'
    return
}

if (-not $Compile) {
    $msgmerge = Get-GettextTool 'msgmerge'
    foreach ($catalog in $catalogs) {
        Invoke-Tool $msgmerge @('--update', '--backup=none', '--quiet', $catalog.FullName, $template)
    }
}

$msgfmt = Get-GettextTool 'msgfmt'
$problems = 0
foreach ($catalog in $catalogs) {
    $language = Split-Path -Leaf (Split-Path -Parent (Split-Path -Parent $catalog.FullName))
    $problems += Test-Placeholder $catalog.FullName $language
    $binary = Join-Path $catalog.DirectoryName 'LunaPlayer.mo'
    Invoke-Tool $msgfmt @('--check-format', "--output-file=$binary", $catalog.FullName)
    $statistics = & $msgfmt '--statistics' '--output-file' $([System.IO.Path]::GetTempFileName()) $catalog.FullName 2>&1
    Write-Host "$language`: $statistics"
}

if ($problems -gt 0) {
    throw "$problems translation(s) do not use the same placeholders as the string they translate."
}
