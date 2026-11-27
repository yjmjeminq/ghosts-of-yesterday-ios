param(
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location -LiteralPath $project
try {
    python tools/compile_story.py --check
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    python tools/check_release.py
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    $arguments = '-batchmode -nographics -quit -projectPath "{0}" -buildTarget Win64 -executeMethod BuildGame.Windows -logFile "{0}\qa\RUNS\build-windows.log"' -f $project
    $process = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode) {
        Get-Content -LiteralPath 'qa/RUNS/build-windows.log' -Tail 25
        exit $process.ExitCode
    }
    Get-Content -LiteralPath 'qa/RUNS/build-windows.log' | Select-String 'GHOSTS_.*_OK'
} finally {
    Pop-Location
}
