param(
    [string]$Version = '1.0.52.2',
    [string]$Configuration = 'Release',
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repo
if (-not $SkipBuild) {
    powershell -ExecutionPolicy Bypass -File .\build\Build-Pano.ps1 -Configuration $Configuration -Pack
}
New-Item -ItemType Directory -Force .\artifacts\release | Out-Null
$zip = ".\artifacts\release\Pano_v$($Version.Replace('.','_'))_Release.zip"
if (Test-Path $zip) { Remove-Item $zip }
$exclude = @('bin','obj','.vs','.git','.agents','artifacts','TestResults')
Get-ChildItem -Force | Where-Object { $exclude -notcontains $_.Name } | Compress-Archive -DestinationPath $zip
Write-Host "Release zip created: $zip" -ForegroundColor Green
