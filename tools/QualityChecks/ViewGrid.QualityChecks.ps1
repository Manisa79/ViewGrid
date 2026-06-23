$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $repo

Write-Host 'Checking duplicate public type declarations...' -ForegroundColor Cyan
$files = Get-ChildItem .\src\ViewGrid -Recurse -Filter *.cs

$typeMap = @{}

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw

    # Supports both file-scoped namespaces:
    #   namespace ViewGrid.Core;
    # and block namespaces:
    #   namespace ViewGrid.Core { ... }
    $namespaceMatches = [regex]::Matches(
        $content,
        '(?m)^\s*namespace\s+([A-Za-z_][A-Za-z0-9_\.]*)\s*(?:;|\{)?'
    )

    $typeMatches = [regex]::Matches(
        $content,
        '(?m)^\s*public\s+(?<mods>(?:new\s+|sealed\s+|abstract\s+|partial\s+|static\s+)*)?(?<kind>class|enum|interface|struct|record)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)'
    )

    foreach ($m in $typeMatches) {
        $namespace = ''
        foreach ($ns in $namespaceMatches) {
            if ($ns.Index -lt $m.Index) {
                $namespace = $ns.Groups[1].Value
            }
        }

        $name = $m.Groups['name'].Value
        $kind = $m.Groups['kind'].Value
        $mods = $m.Groups['mods'].Value
        $isPartial = $mods -match '\bpartial\b'
        $fullName = if ([string]::IsNullOrWhiteSpace($namespace)) { $name } else { "$namespace.$name" }

        if (-not $typeMap.ContainsKey($fullName)) {
            $typeMap[$fullName] = @()
        }

        $lineNumber = ($content.Substring(0, $m.Index) -split "`r?`n").Count

        $typeMap[$fullName] += [pscustomobject]@{
            FullName  = $fullName
            Name      = $name
            Namespace = $namespace
            Kind      = $kind
            IsPartial = $isPartial
            File      = $file.FullName
            Line      = $lineNumber
        }
    }
}

# Duplicate full type names are valid only when every declaration is partial.
# Same short type name in different namespaces is valid C# and should not fail.
$duplicates = @()
foreach ($entry in $typeMap.GetEnumerator()) {
    $declarations = @($entry.Value)
    if ($declarations.Count -gt 1) {
        $allPartial = $true
        foreach ($decl in $declarations) {
            if (-not $decl.IsPartial) {
                $allPartial = $false
                break
            }
        }

        if (-not $allPartial) {
            $duplicates += [pscustomobject]@{
                FullName     = $entry.Key
                Declarations = $declarations
            }
        }
    }
}

if ($duplicates.Count -gt 0) {
    foreach ($dup in $duplicates) {
        Write-Host "Duplicate public type: $($dup.FullName)" -ForegroundColor Red
        foreach ($decl in $dup.Declarations) {
            $partialText = if ($decl.IsPartial) { 'partial' } else { 'non-partial' }
            Write-Host "  $($decl.File):$($decl.Line) [$partialText]"
        }
    }
    throw 'Duplicate public type declarations found.'
}

Write-Host 'Checking known accidental syntax markers...' -ForegroundColor Cyan
$badPatterns = @("'no'", "'left'", "ViewGridColumnProfile : ViewGridColumnProfile")
foreach ($pattern in $badPatterns) {
    $hit = Select-String -Path ($files.FullName) -Pattern $pattern -SimpleMatch -ErrorAction SilentlyContinue
    if ($hit) {
        $hit | ForEach-Object {
            Write-Host "$($_.Path):$($_.LineNumber) $($_.Line)" -ForegroundColor Red
        }
        throw "Bad pattern found: $pattern"
    }
}

Write-Host 'Quality checks completed.' -ForegroundColor Green
