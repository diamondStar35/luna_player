param(
    [string]$SourceRoot = (Join-Path $PSScriptRoot "..\src")
)

$errors = [System.Collections.Generic.List[string]]::new()
$callPattern = [regex]'\bTr(?:Format|PluralFormat)?\s*\('

Get-ChildItem $SourceRoot -Recurse -Filter *.cs -File |
    Where-Object { $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' } |
    ForEach-Object {
        $path = $_.FullName
        $text = [System.IO.File]::ReadAllText($path)
        $lines = [System.IO.File]::ReadAllLines($path)
        $callsByLine = @{}

        foreach ($match in $callPattern.Matches($text)) {
            $lineStart = $text.LastIndexOf("`n", $match.Index)
            $lineEnd = $text.IndexOf("`n", $match.Index)
            if ($lineEnd -lt 0) {
                $lineEnd = $text.Length
            }

            $sourceLine = $text.Substring($lineStart + 1, $lineEnd - $lineStart - 1).TrimStart()
            if ($sourceLine.StartsWith('//')) {
                continue
            }

            $cursor = $match.Index + $match.Length
            while ($cursor -lt $text.Length) {
                while ($cursor -lt $text.Length -and [char]::IsWhiteSpace($text[$cursor])) {
                    $cursor++
                }

                if ($cursor + 1 -lt $text.Length -and $text[$cursor] -eq '/' -and $text[$cursor + 1] -eq '/') {
                    $newline = $text.IndexOf("`n", $cursor + 2)
                    $cursor = if ($newline -lt 0) { $text.Length } else { $newline + 1 }
                    continue
                }

                if ($cursor + 1 -lt $text.Length -and $text[$cursor] -eq '/' -and $text[$cursor + 1] -eq '*') {
                    $commentEnd = $text.IndexOf('*/', $cursor + 2)
                    $cursor = if ($commentEnd -lt 0) { $text.Length } else { $commentEnd + 2 }
                    continue
                }

                break
            }

            while ($cursor -lt $text.Length -and ($text[$cursor] -eq '$' -or $text[$cursor] -eq '@')) {
                $cursor++
            }

            if ($cursor -ge $text.Length -or $text[$cursor] -ne '"') {
                continue
            }

            $index = ([regex]::Matches($text.Substring(0, $match.Index), "`n")).Count
            if (-not $callsByLine.ContainsKey($index)) {
                $callsByLine[$index] = 0
            }

            $callsByLine[$index]++
        }

        foreach ($index in $callsByLine.Keys) {
            if ($callsByLine[$index] -gt 1) {
                $errors.Add("${path}:$($index + 1): put each translated string on its own line with its own translator comment")
            }

            $commentIndex = $index - 1
            $hasTranslatorComment = $false
            while ($commentIndex -ge 0 -and $lines[$commentIndex].TrimStart().StartsWith('//')) {
                if ($lines[$commentIndex] -match '^\s*//\s*Translators:') {
                    $hasTranslatorComment = $true
                }
                $commentIndex--
            }
            if (-not $hasTranslatorComment) {
                $errors.Add("${path}:$($index + 1): translated string has no immediately preceding // Translators: comment")
            }
        }
    }

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Output $_ }
    exit 1
}
