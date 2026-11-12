# Castaway Cove

A cozy, offline, single-player 3D fishing action game for Windows, built with Unity 6000.6.8f1,
the built-in render pipeline and built-in uGUI. Islands, terrain, ocean, sky, UI and audio are
generated in code; the palms, rocks, dock, boat, shop props, NPC and fish use the CC0 Kenney and
Quaternius packs under `Assets/CC0/` (see `CREDITS.md`). No packages were added and nothing is
downloaded at build time. The core loop is catch, kill, collect, sell, upgrade and challenge the
island boss.

## Open and play in the editor

1. Open Unity Hub, choose **Add project from disk**, and pick this repository folder.
2. Unity 6000.6.8f1 opens the project; the first import takes a few minutes.
3. Open `Assets/Scenes/Main.unity` and press **Play**. The game boots into the title screen.

## Build the Windows player

From the repository root:

```
"C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" -batchmode -quit -nographics ^
  -projectPath . -executeMethod CraftBuild.Build -logFile Logs/build.log
```

The player is written to `Build/Game.exe` (Windows x64). The build log ends with
`BUILD_RESULT=Succeeded`. You can also use **Castaway Cove > Build Player** in the editor.

## Play

Launch `Build/Game.exe`. On the title screen choose **Play** for a fresh cove, **Continue** to
load your saved game, or **Quit**. The game runs fully offline.

## Controls

| Action | Key |
|---|---|
| Move | W A S D |
| Look | Mouse |
| Sprint | Left Shift |
| Jump | Space |
| Interact (shop, quest board, boat) | E |
| Equip / put away fishing rod | 1 |
| Equip fists or knife | 2 |
| Equip pistol | 3 |
| Equip shotgun | 4 |
| Attack / shoot | Left mouse |
| Reload | R |
| Eat a fish (heal health and hunger) | F |
| Charge a cast | Hold left mouse, release to throw |
| Hook a bite | Left click while `!! BITE !!` is shown |
| Reel in | Hold left mouse (watch tension), release to ease |
| Cancel fishing | Right mouse |
| Save / Load (manual) | F5 / F9 |
| Pause / resume | Escape |

## The loop

- Equip your rod, face open water, hold left mouse to build a cast, and release.
- Wait for the nibble hint and the bite alert, then click to hook the fish.
- In the reel minigame, hold left mouse to gain line and progress while keeping the tension bar
  below the safe marker. Release to let tension fall; if tension reaches the top, the line snaps.
  Fill the reel bar to land the fish.
- A landed fish is still alive: it flops toward you and attacks. Use fists (2), the knife, pistol
  (3) or shotgun (4), hold left mouse to fight, and R to reload. Bring its HP to zero, then press
  E on the carcass to collect it. Headshots, airborne kills and melee finishers add a kill bonus.
- Watch your HP and food bars. F eats a fish from your bag to heal and refill food; if food hits
  zero, HP drains. If you are knocked out, you wake at the dock and lose 10% of your money.
- Collect fish in your bag, then sell them at the Cove Shop. Use the money at the Weapon Rack next
  to the shop to buy the knife, pistol, shotgun and ammo, plus better rods, bait and a bigger bag.
- Read the quest board: kill 3 creatures, defeat the Spider Crab (quest bait and boss are by the
  shop), and earn $500 to open Isle Three. Beating the Spider Crab and handing in its shell opens
  Isle Two.
- Board the rowboat from the dock (E) and row with WASD. You can fish from the boat too.
- Press Escape for the pause menu (resume, save, load, quit to menu).

## Saving

The game saves JSON to `Application.persistentDataPath/save.json` (normally
`%USERPROFILE%\AppData\LocalLow\Cove Studio\Castaway Cove\save.json`). It autosaves when you sell,
buy, unlock an island, pause or quit, and F5/F9 save or load on demand.

## Command-line options

- `--screenshot <file.png>` — starts the game, draws about 120 frames, saves the window's picture
  to the PNG and exits with code 0. The window is pushed behind other windows and does not need
  focus, and rendering continues while unfocused.
- `--self-test <file.json>` or `-selfTest` — runs the full automated check without human input:
  spawn, movement, cast, forced bite, hook, reel, catch (landed alive), fight, collect, bag,
  sell, buy, save/load and HUD checks.
  It writes a JSON result (per-step pass/fail, timings, final money/bag, errors) to the given path,
  or to `self-test.json` in the persistent data folder when no path is given. The JSON is also
  printed to Unity's player log (standalone Unity redirects process stdout there), and the game
  exits with code 0 only when every step passed.
- `--shot-ui menu|shop|quests|pause` — with `--screenshot`, shows that screen in the captured
  picture (used for checking the UI layout).

## Self-test status

The included build passes all 16 steps. See `test-results/self-test.json` for the latest run:
spawn, move (3.6 m), cast, hook, reel (8.4 s), catch (landed alive), pistol fight, collect, bag,
act, sell, buy, save, load, HUD and boss all passed; final money $315, bag 0, 0 errors. The player also
exits with code 0.

## Contents

- `Assets/Scripts/` — all gameplay, world generation, UI and audio code.
- `Assets/Editor/CraftBuild.cs` — builds `Assets/Scenes/Main.unity` and the Windows player.
- `MECHANICS.md` — every tunable number and where it comes from (all original estimates).
- `mechanics.json` — the same values in machine-readable form.
- `NOTES.md` — what is implemented, what is not, and known limits.
- `shots/` — screenshots captured with `--screenshot`.
