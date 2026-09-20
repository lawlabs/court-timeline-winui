#requires -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('x64', 'ARM64')]
    [string]$Architecture,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') {
    throw 'The WinUI 3 sample must run on Windows.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Install the .NET 10 SDK, then run this script again.'
}
if (-not $Architecture) {
    $Architecture = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64') { 'ARM64' } else { 'x64' }
}
$project = Join-Path $PSScriptRoot 'samples/CourtTimeline.WinUI.Sample/CourtTimeline.WinUI.Sample.csproj'
$runtime = 'win-' + $Architecture.ToLowerInvariant()
& dotnet run --project $project --configuration $Configuration --runtime $runtime "-p:Platform=$Architecture"
if ($LASTEXITCODE -ne 0) { throw "Sample build or launch failed (exit code $LASTEXITCODE)." }
