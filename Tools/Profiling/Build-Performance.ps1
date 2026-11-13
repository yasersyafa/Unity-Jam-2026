param(
    [ValidateSet('StandaloneWindows64','WebGL')][string]$Target = 'StandaloneWindows64',
    [switch]$Development,
    [string]$OutputDirectory = 'Builds/Performance'
)
$ErrorActionPreference = 'Stop'
$unityCli = Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$configuration = if ($Development) { 'Development' } else { 'Release' }
$output = Join-Path $projectPath "$OutputDirectory/$Target/$configuration"
if ($Target -eq 'StandaloneWindows64') { $output = Join-Path $output 'Unity-Jam-2026.exe' }
$buildOptions = if ($Development) { '["Development","DetailedBuildReport"]' } else { '["DetailedBuildReport"]' }
# Keep the project's current scene list and serialized settings; pass build configuration explicitly.
& $unityCli command 'build run' --project-path $projectPath --caller plugin --skill unity-build-validation --target $Target --output-path $output --options $buildOptions --confirm --format json
if ($LASTEXITCODE -ne 0) { throw 'Build request failed.' }
do {
    Start-Sleep -Seconds 5
    $raw = & $unityCli command 'build status' --project-path $projectPath --caller plugin --skill unity-build-validation --format json
    if ($LASTEXITCODE -ne 0) { throw 'Unable to read build status.' }
    # Keep the full report out of terminal output.
    $status = $raw | ConvertFrom-Json
    $status | Select-Object status,result,elapsedMs,totalSizeBytes,totalWarnings,totalErrors | ConvertTo-Json -Compress
} while ($status.status -in @('queued','running','building','pending'))
if ($status.status -in @('failed','cancelled') -or ($status.result -and $status.result -ne 'Succeeded')) { throw "Build did not succeed: $($status.status)" }
