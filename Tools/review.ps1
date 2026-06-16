$BaseBranch = "origin/main"
$ReviewFile = "review_$(Get-Date -Format 'yyyyMMdd_HHmmss').md"

Write-Host ""
Write-Host "===== Build Check ====="

dotnet build --nologo --verbosity quiet

if ($LASTEXITCODE -ne 0)
{
Write-Host ""
Write-Host "Build Failed."
exit 1
}

Write-Host ""
Write-Host "===== Collecting Changed Files ====="

$changedFiles = git diff --name-only "$BaseBranch...HEAD" |
Where-Object {
$_ -match '.(cs|sql)$' -and
$_ -notmatch '.Designer.cs$'
}

if (-not $changedFiles)
{
Write-Host "No Changed Files Found."
exit 0
}

$reviewContent = ""

foreach ($file in $changedFiles)
{
Write-Host "Collecting: $file"

```
$fileDiff = git diff "$BaseBranch...HEAD" -- $file

if ([string]::IsNullOrWhiteSpace($fileDiff))
{
    continue
}

# 토큰 폭주 방지
if ($fileDiff.Length -gt 15000)
{
    Write-Host "Skipping large diff: $file"
    continue
}

$reviewContent += @"
```

### FILE: $file

$fileDiff

"@
}

if ([string]::IsNullOrWhiteSpace($reviewContent))
{
Write-Host "No Reviewable Diff Found."
exit 0
}

$prompt = @"
Review only the provided diff.

Focus only on:

* Runtime exceptions
* Null reference risks
* Resource leaks
* File locking issues
* Thread-safety issues
* SQL performance issues
* Logic bugs
* Missing error handling

Ignore:

* Naming
* Formatting
* Code style
* Refactoring
* Architecture
* Design patterns
* Readability improvements
* Documentation

Rules:

* Report only HIGH or MEDIUM confidence issues.
* Do not speculate.
* Do not mention possible issues without evidence in the diff.
* Keep responses concise.

Output format:

## Findings

### [High|Medium]

File:
Problem:
Recommendation:

If no issues exist, reply exactly:

NO ISSUES FOUND

$reviewContent
"@

$tempFile = Join-Path $env:TEMP "codex-review.txt"

$prompt | Out-File $tempFile -Encoding utf8

Write-Host ""
Write-Host "===== Running Codex Review ====="

Get-Content $tempFile -Raw |
codex exec -o $ReviewFile

Write-Host ""
Write-Host "Review saved -> $ReviewFile"
