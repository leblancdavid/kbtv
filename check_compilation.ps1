#!/usr/bin/env pwsh
# ============================================================================
# KBTV Compilation Check Script (PowerShell)
# Checks C# scripts for compilation errors without doing a full build
# ============================================================================

$ErrorActionPreference = "Stop"

Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "KBTV Compilation Check" -ForegroundColor Cyan
Write-Host "===============================================================================" -ForegroundColor Cyan

# Get the directory where this script is located
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "kbtv"

# Common Unity installation paths
$UnityPaths = @(
    "C:\Program Files\Unity\Hub\Editor\6000.3.1f1\Editor\Unity.exe"
    "C:\Program Files\Unity\Hub\Editor\6000.2.0f1\Editor\Unity.exe"
    "C:\Program Files\Unity\2023.2.0f1\Editor\Unity.exe"
    "C:\Program Files\Unity\2022.3 LTS\Editor\Unity.exe"
    "C:\Program Files\Unity\Editors\6000.3.1f1\Editor\Unity.exe"
    "${env:ProgramFiles}\Unity\Hub\Editor\6000.3.1f1\Editor\Unity.exe"
    "${env:ProgramFiles}\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe"
)

$UnityPath = $null
foreach ($path in $UnityPaths) {
    if (Test-Path $path) {
        $UnityPath = $path
        break
    }
}

# Check for UNITY_PATH environment variable
if (-not $UnityPath -and $env:UNITY_PATH) {
    if (Test-Path $env:UNITY_PATH) {
        $UnityPath = $env:UNITY_PATH
    }
}

if (-not $UnityPath) {
    Write-Error "Unity.exe not found in common locations.`nPlease ensure Unity 2022.3 LTS or newer is installed,`nor set the UNITY_PATH environment variable." -ErrorAction Continue
    exit 1
}

Write-Host "Using Unity: $UnityPath" -ForegroundColor Gray
Write-Host "Project: $ProjectDir" -ForegroundColor Gray
Write-Host ""

$TempLog = Join-Path $env:TEMP "kbtv_compile.log"

# Run Unity in batchmode to check compilation
$process = Start-Process -FilePath $UnityPath -ArgumentList @(
    "-projectPath `"$ProjectDir`"",
    "-batchmode",
    "-quit",
    "-executeMethod KBTV.Editor.CompilationCheck.Check",
    "-logFile `"$TempLog`""
) -NoNewWindow -Wait -PassThru

$exitCode = $process.ExitCode

# Display the log if there were errors
if ($exitCode -ne 0 -and (Test-Path $TempLog)) {
    Write-Host ""
    Write-Host "--- Compilation Log ---" -ForegroundColor Yellow
    Get-Content $TempLog | Select-Object -Last 50
}

if ($exitCode -eq 0) {
    Write-Host ""
    Write-Host "===============================================================================" -ForegroundColor Green
    Write-Host "SUCCESS: No compilation errors found" -ForegroundColor Green
    Write-Host "===============================================================================" -ForegroundColor Green
    exit 0
} elseif ($exitCode -eq 1) {
    Write-Host ""
    Write-Host "===============================================================================" -ForegroundColor Red
    Write-Host "FAILED: Compilation errors found" -ForegroundColor Red
    Write-Host "===============================================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check the log above for details." -ForegroundColor Gray
    Write-Host "Full log: $TempLog" -ForegroundColor Gray
    exit 1
} else {
    Write-Host ""
    Write-Host "===============================================================================" -ForegroundColor Red
    Write-Host "ERROR: Compilation check failed (exit code: $exitCode)" -ForegroundColor Red
    Write-Host "===============================================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check Unity log for details." -ForegroundColor Gray
    Write-Host "Full log: $TempLog" -ForegroundColor Gray
    exit 2
}
