param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',
    [switch]$Strict,
    [switch]$Pack,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repo

Write-Host '== ViewGrid clean ==' -ForegroundColor Cyan
dotnet clean .\ViewGrid.sln -c $Configuration

Write-Host '== ViewGrid restore ==' -ForegroundColor Cyan
dotnet restore .\ViewGrid.sln

$env:VIEWGRID_STRICT = if ($Strict) { 'true' } else { 'false' }
Write-Host "== ViewGrid build ($Configuration) ==" -ForegroundColor Cyan
dotnet build .\ViewGrid.sln -c $Configuration --no-restore /p:CI=true

if (-not $SkipTests) {
    if (Test-Path .\tests) {
        Write-Host '== ViewGrid tests ==' -ForegroundColor Cyan
        dotnet test .\tests -c $Configuration --no-build --logger "trx;LogFileName=viewgrid-tests.trx"
    }
    if (Test-Path .\tools\QualityChecks\ViewGrid.QualityChecks.ps1) {
        Write-Host '== ViewGrid quality checks ==' -ForegroundColor Cyan
        powershell -ExecutionPolicy Bypass -File .\tools\QualityChecks\ViewGrid.QualityChecks.ps1
    }
}

if ($Pack) {
    Write-Host '== ViewGrid pack ==' -ForegroundColor Cyan
    New-Item -ItemType Directory -Force .\artifacts\nuget | Out-Null
    dotnet pack .\src\ViewGrid\ViewGrid.csproj -c $Configuration --no-build -o .\artifacts\nuget
}

Write-Host 'ViewGrid build pipeline completed.' -ForegroundColor Green
