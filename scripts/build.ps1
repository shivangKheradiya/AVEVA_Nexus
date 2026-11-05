# Builds AVEVA_Nexux_Addin.dll with the Roslyn csc.exe from VS BuildTools (the .NET Framework csc is C# 5 and cannot
# compile this code). Output: <Out>\AVEVA_Nexux_Addin.dll (+ Newtonsoft.Json.dll next to it).
param(
    [string]$E3dDir = 'C:\Program Files (x86)\AVEVA\Everything3D3.1',
    [string]$Out
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $Out) { $Out = Join-Path $repo 'out' }
$csc = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
if (-not (Test-Path $csc)) { throw "Roslyn csc.exe not found: $csc" }

$src = Join-Path $repo 'AVEVA_Nexus\AVEVA_Nexus'
$newtonsoft = Join-Path $repo 'AVEVA_Nexus\packages\Newtonsoft.Json.13.0.1\lib\net45\Newtonsoft.Json.dll'
New-Item -ItemType Directory -Force $Out | Out-Null

$files = Get-ChildItem $src -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } | ForEach-Object { '"' + $_.FullName + '"' }
$rsp = Join-Path $Out 'build.rsp'
$refs = 'Aveva.ApplicationFramework', 'Aveva.Core.Commands', 'Aveva.Core.Database', 'Aveva.Core.Utilities', 'PMLNet' |
    ForEach-Object { '/r:"' + (Join-Path $E3dDir "$_.dll") + '"' }
@(
    '/nologo', '/target:library', '/platform:x86', "/out:`"$(Join-Path $Out 'AVEVA_Nexux_Addin.dll')`"",
    "/r:`"$newtonsoft`"", '/r:System.Configuration.dll', '/r:System.Net.Http.dll'
) + $refs + $files | Set-Content $rsp -Encoding ASCII

& $csc "@$rsp"
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed (csc exit $LASTEXITCODE)"; exit $LASTEXITCODE }
Copy-Item $newtonsoft $Out -Force
"Built $(Join-Path $Out 'AVEVA_Nexux_Addin.dll')"
