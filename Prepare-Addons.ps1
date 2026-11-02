param(
    [string]$GamePath = 'D:\SteamLibrary\steamapps\common\Rain World',
    [string]$MeadowPluginPath = 'D:\SteamLibrary\steamapps\workshop\content\312520\3388224007\plugins\Rain Meadow.dll'
)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Prepare-Workshop.ps1') -GamePath $GamePath -MeadowPluginPath $MeadowPluginPath
$addons = @(
    @{ Folder = 'chained-together'; Project = 'AbsoluteChainedTogether'; Assembly = 'AbsoluteFriends.ChainedTogether' },
    @{ Folder = 'resonance'; Project = 'AbsoluteResonance'; Assembly = 'AbsoluteFriends.Resonance' },
    @{ Folder = 'friendship-editor'; Project = 'AbsoluteFriendshipEditor'; Assembly = 'AbsoluteFriends.FriendshipEditor' }
)

foreach ($addon in $addons) {
    $addonPath = Join-Path $PSScriptRoot "external\$($addon.Folder)"
    $libraryPath = Join-Path $addonPath 'lib'
    New-Item -ItemType Directory -Path $libraryPath -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'lib') -Filter '*.dll' | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $libraryPath -Force
    }
    foreach ($dependency in 'AbsoluteFriends', 'AbsoluteFriends.Core', 'AbsoluteFriends.Manager') {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot "artifacts\plugins\$dependency.dll") -Destination $libraryPath -Force
    }
    & dotnet build (Join-Path $addonPath "src\$($addon.Project).csproj") -c Release --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $($addon.Folder)" }

    $packagePath = Join-Path $PSScriptRoot "artifacts\addon-packages\$($addon.Folder)"
    $pluginPath = Join-Path $packagePath 'plugins'
    New-Item -ItemType Directory -Path $pluginPath -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $addonPath 'assets') | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $packagePath -Recurse -Force
    }
    $assemblyPath = Join-Path $addonPath "artifacts\plugins\$($addon.Assembly).dll"
    [void][System.Reflection.AssemblyName]::GetAssemblyName($assemblyPath)
    Copy-Item -LiteralPath $assemblyPath -Destination $pluginPath -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination $packagePath -Force
    $metadata = Get-Content -LiteralPath (Join-Path $packagePath 'modinfo.json') -Raw | ConvertFrom-Json
    if ($metadata.requirements -notcontains 'pjhgamma.absolutefriends') { throw 'Missing base mod requirement.' }
    if (@(Get-ChildItem -LiteralPath $pluginPath -Filter '*.dll').Count -ne 1) { throw 'Unexpected bundled dependencies.' }
    Write-Output "Addon package ready: $($metadata.name) $($metadata.version) - $packagePath"
}
