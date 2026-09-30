param(
    [Parameter(Mandatory = $true)][string]$Godot,
    [switch]$VerifyRepeatability
)
$ErrorActionPreference = 'Stop'
$project = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not (Test-Path -LiteralPath $project)) { throw 'Project directory missing' }
function Invoke-Bake {
    # Always reconstruct inputs from original assets before post-processing.
    foreach ($script in @('rebake_vern_clips.gd', 'bake_basic_talk_mpfb.gd', 'author_vern_prop_actions.gd')) {
        & $Godot --headless --path $project --script "res://Tools/modelgen/$script"
        if ($LASTEXITCODE -ne 0) { throw "Bake failed: $script" }
    }
}
function Get-ClipHashes {
    foreach ($clip in @('idle_breathing', 'talk_calm', 'talking_default', 'smoking', 'drink_coffee')) {
        (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $project "assets/models3d/characters/vern/animations/${clip}_mpfb.tres")).Hash
    }
}
Invoke-Bake
if ($VerifyRepeatability) {
    $before = @(Get-ClipHashes)
    Invoke-Bake
    $after = @(Get-ClipHashes)
    if (Compare-Object $before $after -SyncWindow 0) { throw 'Repeated clean bakes differ' }
    'Repeatability verified: all five clip SHA256 hashes match.'
}
