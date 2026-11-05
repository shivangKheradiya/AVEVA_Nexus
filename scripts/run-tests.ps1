# Unit tests: compiles tests\Nexus.Tests\*.cs together with the AVEVA-free sources and runs them.
# No E3D and no NuGet needed. Exit code != 0 if any check fails.
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$out = Join-Path $repo 'out'
$csc = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$src = Join-Path $repo 'AVEVA_Nexus\AVEVA_Nexus'
$newtonsoft = Join-Path $repo 'AVEVA_Nexus\packages\Newtonsoft.Json.13.0.1\lib\net45\Newtonsoft.Json.dll'
New-Item -ItemType Directory -Force $out | Out-Null

# Only folders/files that must never reference Aveva.* namespaces.
$pure = @()
foreach ($d in 'Exceptions', 'Validation', 'Models', 'Query') {
    $p = Join-Path $src $d
    if (Test-Path $p) { $pure += (Get-ChildItem $p -Filter *.cs).FullName }
}
$ro = Join-Path $src 'Config\ReadOnlyMode.cs'
if (Test-Path $ro) { $pure += $ro }
$tests = (Get-ChildItem (Join-Path $repo 'tests\Nexus.Tests') -Filter *.cs).FullName

$exe = Join-Path $out 'Nexus.Tests.exe'
$files = ($pure + $tests) | ForEach-Object { '"' + $_ + '"' }
$rsp = Join-Path $out 'tests.rsp'
@('/nologo', '/target:exe', '/platform:x86', "/out:`"$exe`"", "/r:`"$newtonsoft`"") + $files | Set-Content $rsp -Encoding ASCII
& $csc "@$rsp"
if ($LASTEXITCODE -ne 0) { Write-Error "Test build failed"; exit $LASTEXITCODE }
Copy-Item $newtonsoft $out -Force
& $exe
exit $LASTEXITCODE
