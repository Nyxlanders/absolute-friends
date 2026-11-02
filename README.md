# Absolute Friends: Refriended

Refriended is Nyxlanders' repackaging of Absolute Friends 0.3.0 by pjhgamma,
with a new cover and display name. Gameplay is unchanged in this edition.

- Original: https://github.com/pjhgamma/absolute-friends
- Fork: https://github.com/Nyxlanders/absolute-friends
- License: MIT; see LICENSE. Original copyright and credits are preserved.

## Build the Workshop package

Requirements: .NET SDK 10, Rain World with BepInEx, and Rain Meadow installed.
The build uses assemblies from your installed game; these dependencies are not
included in the published package.

Run `./Prepare-Workshop.ps1` in PowerShell. Optional parameters:
`-GamePath` and `-MeadowPluginPath` override the default D: Steam library paths.
The completed package is in `artifacts/workshop/absolute-friends-refriended`.
It includes the main plugin, ten built-in modules, assets, and license notices.
External addons such as Resonance and Friendship Editor are separate mods.

Run `./Prepare-Addons.ps1` to also build Chained Together, Resonance, and
Friendship Editor. Separate installable folders are generated under
`artifacts/addon-packages`. Copy each folder into the game's mods directory,
then restart the game and enable the desired addons in Remix alongside the
base mod. The developer demonstration addon Example is not bundled.

## Local test and upload

Copy the package into `RainWorld_Data/StreamingAssets/mods` inside Rain World.
Open Remix and check Absolute Friends: Refriended before publishing with the
game's Workshop uploader. Test a campaign and the Remix options first.
WORKSHOP-DESCRIPTION.txt contains suggested listing text.

This edition keeps the original mod ID and plugin IDs for compatibility with
existing settings and addons. Use only one edition at a time. A local copy can
take precedence over the subscribed original. Remove the local folder to return
to the Workshop copy. Create a new Workshop item under your account; the original
author's item ID is not included in this package.
