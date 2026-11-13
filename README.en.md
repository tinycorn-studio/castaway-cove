# Castaway Cove

A single-player, offline 3D fishing action game for Windows. Catch fish, finish the living
catch with weapons, sell it, upgrade gear and fight five bosses across five islands to get
home. Built with Unity 6000.6.8f1 (built-in pipeline, uGUI); code, UI and audio are original,
models are CC0 assets from Kenney and Quaternius.

## Requirements

- Windows 10/11 x64.
- Unity 6000.6.8f1 (Unity Hub) to open/build.
- The built player runs offline with no extra installs.

## Build / run

Open this repository in Unity Hub, open `Assets/Scenes/Main.unity` and press **Play**.

Build the Windows player from the repository root:

```
"C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath . -executeMethod CraftBuild.Build -logFile Logs/build.log
```

The player is written to `Build/Game.exe`. The editor also has **Castaway Cove > Build Player**.

## Features

- Fishing loop: cast, nibble/bite, hook, tension/stamina reel minigame; rods, bait, bag.
- Living catch: landed fish have HP, flop/charge and attack; kill them, then collect the carcass.
- Survival: HP and food, eat fish to heal, knock-out and dock respawn.
- Weapons: fists, knife, pistol, shotgun and dynamite; ammo, reload, headshot/airborne/melee/longshot bonuses.
- Five islands and five bosses: Spider Crab, Giant Piranha, Pufferfish, Albatross, Mutated Whale.
- Quest chain, boat keys and radar; each boss trophy opens the next island.
- Economy: sell fish, rod/bait/bag shop, Weapon Rack for weapons/ammo/dynamite.
- Cooking: risky grill (1.5x, burn chance) and safe lava cooking on Isle Five.
- Collection: 10% Drip variants (8x value), Fishipedia (Tab), achievements.
- Reel of Fortune gacha, roulette and slots with in-game money.
- Shootable seagulls, Normal/Hard difficulty, JSON save/load, pause and main menu.

## Controls

| Action | Key |
|---|---|
| Move / look | WASD / mouse |
| Sprint / jump | Shift / Space |
| Interact | E |
| Rod | 1 |
| Fists or knife | 2 |
| Pistol | 3 |
| Shotgun | 4 |
| Throw dynamite | 5 |
| Attack / shoot | Left mouse |
| Reload | R |
| Eat fish | F |
| Fishipedia | Tab |
| Save / load | F5 / F9 |
| Pause | Escape |

## Structure

- `Assets/Scripts/Core`: bootstrap, data, save/load, audio, self-test.
- `Assets/Scripts/Player`, `Fishing`, `Combat`, `UI`, `World`.
- `Assets/Editor`: CC0 import and player build.
- `Assets/CC0`: source models/textures and licenses; `Assets/Resources/CC0`: generated prefabs/materials.
- `shots/`, `test-results/`, `MECHANICS.md`, `mechanics.json`.

## Save / command line

Save path: `%USERPROFILE%\AppData\LocalLow\Cove Studio\Castaway Cove\save.json`.
Flags: `-selfTest` / `--self-test <file>`, `--screenshot <file> --shot-ui <screen>`.

## Known limits

- No multiplayer, music, settings menu or gamepad.
- Cooking/gacha/casino are simplified; Killscore has no 360/no-scope/dogfight.
- No character customization; skins tint weapons only.
- All islands share one shop; boss AI is a shared charge/stun frame.
- Balance values are initial estimates.

## Versions

- **0.2.0** (2026-11-12): five islands and bosses, quests, radar, cooking, Drip, Fishipedia,
  gacha/casino, difficulty, dynamite, seagulls; 18-step self-test.
- **0.1.0** (2026-11-12): initial fishing/combat prototype. See `CHANGELOG.md`.

## Download

Latest build: [GitHub Releases](https://github.com/tinycorn-studio/castaway-cove/releases/latest).
Build output and installers are not committed.

## Credits

CC0 assets and licenses: `CREDITS.md`, `Assets/CC0/SOURCES.md`, `Assets/CC0/Licenses/`.
Original code and content: TinyCorn Studio, MIT (`LICENSE`).
