# Castaway Cove — implementation notes

## Implemented

### World and presentation
- One bootstrap scene (`Assets/Scenes/Main.unity`) with a single `GameBoot` object; world, player,
  UI and audio are built at runtime from C#.
- Five islands: Cove Island, Isle Two, Isle Three, Isle Four and Isle Five, each with procedural
  beach/grass terrain, palms, rocks, plants, a name sign and a boss altar. Isle Five has a lava vent.
- CC0 visual packs under `Assets/CC0/` with licenses: Kenney Nature/Pirate/Survival/Mini Characters
  and Quaternius Animated Fish. `Assets/Editor/CC0Import.cs` configures FBX import and writes
  palette materials/prefabs under `Assets/Resources/CC0/`.
- Animated ocean, sky, sun, cloud clusters, fog; all audio is synthesized at runtime.

### Fishing and economy
- Equip rod with 1; charge/release cast; nibble and bite cues; hook; tension/stamina reel minigame.
- Eight species with island pools (`minIsland`): 1/1/2/3/3/4/5/5; 3% rare (5x) and 10% Drip (8x).
- Bag with capacity; sell-all; rods, bait and bag upgrades; Weapon Rack sells knife, pistol,
  shotgun, ammo and dynamite.
- Fish are logged in the Fishipedia on catch; Drip fish feed the Reel of Fortune.

### Combat and survival
- Landed fish become live targets with tier-based HP, hop/charge movement and contact damage.
- Player HP/food; F eats a bag fish (cooked fish heal more); death respawns at the dock with a 10% money loss.
- Fists, knife, pistol and shotgun with ammo/reload; dynamite (key 5) explodes for area damage.
- Kill bonuses: headshot +25%, airborne +50%, melee +5%, longshot +30% value.
- Shootable seagulls orbit the start island and pay $15.

### Bosses and progression
- Spider Crab (start island) drops a shell; handing it in gives boat keys, the radar, $300 and Isle Two.
- Giant Piranha (Isle Two), Pufferfish (Isle Three), Albatross (Isle Four) and Mutated Whale
  (Isle Five) each drop a trophy that opens the next island; the whale ends the story.
- Boss AI: telegraphed charge, dodge window, self-stun, 3D target bar, contact damage.
- Quest board shows the five boss objectives; radar shows every island's distance and lock state.
- Boat is locked until the crab shell is handed in; the current boundary expands per island.

### Cooking, collection and extras
- Grill near the shop: cook all raw bag fish for 1.5x value, 15% burn risk. Isle Five lava cooks safely.
- Fishipedia (Tab) lists caught/missing species and achievements; achievements also toast when earned.
- Reel of Fortune: spin with a Drip fish or $100 for a weapon skin; roulette ($50) and slots ($25).
- Main-menu difficulty toggle: Hard gives newly spawned creatures 1.4x HP and 1.3x damage.
- Save/load JSON stores money, rods, bait, bag, HP/food, weapons/ammo, kills, islands, boss flags,
  discovered species, achievements, skin, difficulty, dynamite, player and boat pose.

### Tooling contract
- `--screenshot <file.png>` renders ~120 frames and saves a PNG; `--shot-ui` selects menu, shop,
  quests, pause, combat, boss, weapons, fishipedia, grill, fortune, piranha or radar.
- `--self-test` / `-selfTest` runs the 18-step check with a fixed seed and exits 0 only if all pass:
  spawn, move, cast, hook, reel, catch alive, fight, collect, bag, sell, buy, save, load, HUD,
  Spider Crab, four-island boss chain, cooking/gacha/dynamite/difficulty/Fishipedia.
- Latest run: all 18 steps passed; build 0 errors.

## Known limits
- No multiplayer, music, settings menu or gamepad.
- Cooking/gacha/casino are simplified; no 360/no-scope/dogfight kill multipliers.
- No character customization; skins tint weapon parts only.
- All islands share the start shop; no unique island shop/lure inventory.
- Boss AI is a shared charge/stun frame with different stats/models.
- Balance values are estimates; no long playtest tuning yet.

## Technical notes
- Built-in render pipeline and uGUI; no extra packages.
- Canvases are ScreenSpaceCamera at a small plane distance for screenshot capture.
- `CraftBuild` adds Standard/UI/CC0 shaders to Always Included Shaders and rebuilds the scene.
- Generated `Build/`, `Library/`, `Logs/`, `UserSettings/` and IDE files are gitignored.
