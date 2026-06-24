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

Write-Host '== Pano clean ==' -ForegroundColor Cyan
dotnet clean .\Taylan.Pano.sln -c $Configuration

Write-Host '== Pano restore ==' -ForegroundColor Cyan
dotnet restore .\Taylan.Pano.sln

$env:PANO_STRICT = if ($Strict) { 'true' } else { 'false' }
Write-Host "== Pano build ($Configuration) ==" -ForegroundColor Cyan
dotnet build .\Taylan.Pano.sln -c $Configuration --no-restore /p:CI=true

if (-not $SkipTests) {
    if (Test-Path .\tests) {
        Write-Host '== Pano tests ==' -ForegroundColor Cyan
        dotnet test .\tests -c $Configuration --no-build --logger "trx;LogFileName=pano-tests.trx"
    }
    if (Test-Path .\tools\QualityChecks\Taylan.Pano.QualityChecks.ps1) {
        Write-Host '== Pano quality checks ==' -ForegroundColor Cyan
        powershell -ExecutionPolicy Bypass -File .\tools\QualityChecks\Taylan.Pano.QualityChecks.ps1
    }
}

if ($Pack) {
    Write-Host '== Pano pack ==' -ForegroundColor Cyan
    New-Item -ItemType Directory -Force .\artifacts\nuget | Out-Null
    dotnet pack .\src\Taylan.Pano\Taylan.Pano.csproj -c $Configuration --no-build -o .\artifacts\nuget
}

Write-Host 'Pano build pipeline completed.' -ForegroundColor Green
