<#
.SYNOPSIS
  Runs the KBTV GoDotTest suite with the correct Godot 4.6 mono engine.

.DESCRIPTION
  The project targets Godot 4.6 (see config/features in project.godot), but the
  engine commonly installed in Program Files is 4.5.1. Passing --run-tests to an
  engine that is NOT 4.6 (or to a project whose main scene is the game, via the
  bare `godot --run-tests` invocation) boots the game scene instead of the test
  harness and hangs indefinitely.

  This script locates a Godot 4.6 mono binary, launches the GoDotTest scene
  (test/Tests.tscn) directly, and exits non-zero if any test fails.

.PARAMETER Godot
  Optional explicit path to a Godot 4.6 mono .exe. Auto-detected when omitted.

.PARAMETER Filter
  Optional GoDotTest filter, e.g. "RoomStateManager" or "ResultTests".

.PARAMETER Coverage
  Pass --coverage to the engine (used by the coverage/report pipeline).

.EXAMPLE
  powershell -File run-tests.ps1
  powershell -File run-tests.ps1 -Filter RoomStateManager
  powershell -File run-tests.ps1 -Godot "C:\Godot\Godot_v4.6.3-stable_mono_win64_console.exe"
#>
param(
  [string]$Godot = "",
  [string]$Filter = "",
  [switch]$Coverage
)

$ErrorActionPreference = "Stop"
$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Test-Version46 {
  param([string]$Path)
  return $Path -match "4\.6"
}

function Resolve-Godot {
  param([string]$Path = "")

  if ($Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
      throw "Godot path not found: $Path"
    }
    return $Path
  }

  if ($env:GODOT) {
    if ((Test-Path -LiteralPath $env:GODOT) -and (Test-Version46 $env:GODOT)) {
      return $env:GODOT
    }
    Write-Host "WARNING: GODOT env var ('$env:GODOT') is not a Godot 4.6 build; ignoring it."
    Write-Host "         Set GODOT to the 4.6 mono console exe, or pass -Godot, or remove GODOT to auto-detect."
  }

  $bases = @(
    "$env:USERPROFILE\AppData\Local\Temp\opencode\godot463\Godot_v4.6.3-stable_mono_win64",
    "$env:USERPROFILE\AppData\Local\Temp\opencode\godot46\Godot_v4.6.3-stable_mono_win64",
    "D:\Software\Godot",
    "C:\Program Files\Godot"
  )

  foreach ($base in $bases) {
    if (-not (Test-Path -LiteralPath $base)) { continue }
    $exe = Get-ChildItem -LiteralPath $base -Filter "Godot*.exe" -Recurse -ErrorAction SilentlyContinue |
      Where-Object { (Test-Version46 $_.FullName) -and ($_.Name -match "mono") -and ($_.Name -match "console") } |
      Sort-Object Name -Descending |
      Select-Object -First 1
    if ($exe) { return $exe.FullName }
  }

  throw "Could not find a Godot 4.6 mono binary. Pass -Godot <path> explicitly or set GODOT to a 4.6 mono console exe."
}

$godot = Resolve-Godot $Godot
Write-Host "Using Godot: $godot"

$args = @("--path", $ProjectDir, "--quit-on-finish")
if ($Coverage) { $args += "--coverage" }
if ($Filter) { $args += "--run-tests=$Filter" } else { $args += "--run-tests" }
$args += "res://test/Tests.tscn"

Write-Host "Running: & `"$godot`" $($args -join ' ')"
Write-Host ""

$out = & $godot @args 2>&1
$out | ForEach-Object { Write-Host $_ }

$result = $out -join "`n"
if ($result -match "Passed: (\d+) \| Failed: (\d+) \| Skipped: (\d+)") {
  $passed = [int]$Matches[1]
  $failed = [int]$Matches[2]
  $skipped = [int]$Matches[3]
  Write-Host ""
  Write-Host ("RESULTS: Passed: {0} | Failed: {1} | Skipped: {2}" -f $passed, $failed, $skipped)
  if ($failed -gt 0) { exit 1 }
  exit 0
} else {
  Write-Host ""
  Write-Host "Could not parse test summary from engine output (see above)."
  exit $LASTEXITCODE
}