# Builds the addin and restarts the local E3D (TTY) session with it.
# <Target> is the local runtime folder that holds Start-Nexus.ps1, Stop-Nexus.ps1, config\ and bin\
# (e.g. D:\ProjectesRandom\AVEVA_Nexus_local). -Writable starts the session with NEXUS_READONLY=0.
param(
    [Parameter(Mandatory = $true)][string]$Target,
    [string]$Project = 'CAR',
    [int]$Port = 5001,
    [switch]$Writable,
    [string]$E3dDir = 'C:\Program Files (x86)\AVEVA\Everything3D3.1'
)
$ErrorActionPreference = 'Stop'
$out = Join-Path (Split-Path -Parent $PSScriptRoot) 'out'

& (Join-Path $PSScriptRoot 'build.ps1') -E3dDir $E3dDir -Out $out
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

& (Join-Path $Target 'Stop-Nexus.ps1') -Project $Project
Start-Sleep -Seconds 3
Copy-Item (Join-Path $out 'AVEVA_Nexux_Addin.dll') (Join-Path $Target 'bin') -Force
Copy-Item (Join-Path $out 'Newtonsoft.Json.dll') (Join-Path $Target 'bin') -Force

$startArgs = @{ Project = $Project; NoWait = $true }
if ($Writable) { $startArgs['Writable'] = $true }
& (Join-Path $Target 'Start-Nexus.ps1') @startArgs

$deadline = (Get-Date).AddSeconds(120)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 3
    try {
        $r = Invoke-RestMethod "http://localhost:$Port/api/Health" -TimeoutSec 3
        if ($r.Success) { "UP (http://localhost:$Port/)"; exit 0 }
    } catch { }
}
Write-Error "Timeout: Nexus did not answer on port $Port within 120 s"
exit 1
