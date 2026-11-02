param(
    [string]$GamePath = 'D:\SteamLibrary\steamapps\common\Rain World',
    [string]$MeadowPluginPath = 'D:\SteamLibrary\steamapps\workshop\content\312520\3388224007\plugins\Rain Meadow.dll'
)

$ErrorActionPreference = 'Stop'
$repoPath = $PSScriptRoot
$libraryPath = Join-Path $repoPath 'lib'
New-Item -ItemType Directory -Path $libraryPath -Force | Out-Null
$references = @(
    (Join-Path $GamePath 'BepInEx\utils\PUBLIC-Assembly-CSharp.dll'),
    (Join-Path $GamePath 'BepInEx\plugins\HOOKS-Assembly-CSharp.dll'),
    (Join-Path $GamePath 'BepInEx\core\BepInEx.dll'),
    (Join-Path $GamePath 'BepInEx\core\Mono.Cecil.dll'),
    (Join-Path $GamePath 'BepInEx\core\MonoMod.Utils.dll'),
    (Join-Path $GamePath 'BepInEx\core\MonoMod.RuntimeDetour.dll'),
    (Join-Path $GamePath 'RainWorld_Data\Managed\UnityEngine.dll'),
    (Join-Path $GamePath 'RainWorld_Data\Managed\UnityEngine.CoreModule.dll'),
    (Join-Path $GamePath 'RainWorld_Data\Managed\UnityEngine.InputLegacyModule.dll'),
    $MeadowPluginPath
)
foreach ($reference in $references) {
    Copy-Item -LiteralPath $reference -Destination $libraryPath
}

& dotnet build (Join-Path $repoPath 'AbsoluteFriends.slnx') -c Release --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed; no package was prepared.' }

$packagePath = Join-Path $repoPath 'artifacts\workshop\absolute-friends-refriended'
$pluginPath = Join-Path $packagePath 'plugins'
New-Item -ItemType Directory -Path $pluginPath -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $repoPath 'assets') | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $packagePath -Recurse -Force
}
$modules = '', '.Core', '.Manager', '.Meadow', '.Visualizer', '.General', '.Items', '.Players', '.Creatures', '.Iterators', '.Pilgrimage'
foreach ($module in $modules) {
    $fileName = "AbsoluteFriends$module.dll"
    $assemblyPath = Join-Path $repoPath "artifacts\plugins\$fileName"
    [void][System.Reflection.AssemblyName]::GetAssemblyName($assemblyPath)
    Copy-Item -LiteralPath $assemblyPath -Destination $pluginPath -Force
}
foreach ($notice in 'LICENSE', 'NOTICE.txt') {
    Copy-Item -LiteralPath (Join-Path $repoPath $notice) -Destination $packagePath -Force
}
$metadata = Get-Content -LiteralPath (Join-Path $packagePath 'modinfo.json') -Raw | ConvertFrom-Json
if ($metadata.name -ne 'Absolute Friends: Refriended') { throw 'Unexpected package name.' }
if ((Get-Item -LiteralPath (Join-Path $packagePath 'thumbnail.png')).Length -ge 1MB) {
    throw 'The Workshop preview image must be smaller than 1 MB.'
}
Write-Output "Workshop package ready: $packagePath"
