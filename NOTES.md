# Castaway Cove - implementation notes

## Implemented

### World and presentation
- One bootstrap scene (`Assets/Scenes/Main.unity`) with a single `GameBoot` object; the entire
  world, player, UI and audio are built at runtime from C#.
- Three low-poly islands generated as procedural mesh rings (beach ring + grass cap), keeping the
  original walkable terrain. Palms, rocks, grass/plants and cliffs now use the CC0 Kenney Nature
  and Pirate models, scattered with varied rotation and scale. The start island has a CC0 dock, an
  open shop stall with counter/props/NPC, a signpost quest board, a fishing-spot prop and a CC0
  rowboat.
- Animated low-poly ocean mesh with gentle waves, a solid warm sky, sun sphere, puffy cloud
  clusters, directional sunlight, flat ambient light and soft distance fog.
- Audio clips are still synthesized at runtime with `AudioClip.Create`. All code, UI and sounds are
  original; the visual models are the CC0 packs described below.

### CC0 visual packs
- Source models live under `Assets/CC0/` with their licenses and `SOURCES.md`; credits are in
  `CREDITS.md`. No packages were added and nothing is downloaded at build time.
- `Assets/Editor/CC0Import.cs` runs inside the build (and from the Castaway Cove menu). It configures
  each FBX with Unity's native importer at scale 1 and standard material/texture import, creates
  palette/colormap materials, bakes the Quaternius fish display meshes, and writes prefabs under
  `Assets/Resources/CC0/` for runtime loading.
- Kenney Pirate/Survival/Mini models use their matching `Textures/colormap.png`. Nature models and
  the Quaternius fish use flat palette colours (a double-sided lit material; the held fish uses an
  unlit tint so species colours read clearly).
- Placement is fitted per model at runtime (height/footprint) with varied yaw, so the very different
  native FBX scales display consistently. Simple Box/Capsule colliders are added only to obstacles
  the player can walk into: large rocks/cliffs on the far islands, the shop counter/crates/barrel/
  chest/plank pile, the signpost, the NPC and the dock walkway. The boat keeps its trigger.
- The Quaternius Swim clips import and are stored on the source prefabs; the held/catch display uses
  a baked Swim-pose mesh because the skinned armature does not render reliably at held scale. The
  Kenney survival fish and fish-large are static trophies on the shop fence.

### Player and movement
- First-person CharacterController: WASD, mouse look with pitch clamp, sprint, jump, gravity,
  slow buoyant swimming with splash on entry and while swimming, footsteps, head bob.
- Interaction via E raycast (shop, quest board, rowboat) with an on-screen prompt.
- Legacy Input Manager (`Input.GetAxisRaw`, keys, mouse buttons), Active Input Handling set to
  old input (default in this project), so no Input System package is needed.

### Fishing
- Equip/put away rod with 1; visible rod model.
- Hold-left-mouse charge bar and release to throw a bobber with an arcing flight and a visible
  three-point line.
- Nibble hint, bite alert with audio, timed bite window, hook by click.
- Reel minigame: hold to reel, tension rises with fish pulls and snaps at maximum; releasing
  reduces tension, fish stamina and reel progress are tracked; catch when progress fills.
- Works from shore, the dock and the rowboat. Right-click cancels.
- Eight species across common/uncommon/rare/epic with weights and table weights driven by an
  embedded JSON fish table, random weight and value, and a 3% rare variant worth 5x.
- Bait is consumed per cast when available and improves bite timing and rarity odds.

### Inventory, economy, shop, quests
- Bag with capacity and a bag-full message; sell-all in the shop; money.
- Shop lists all three rod tiers (basic owned, two upgrades), bait packs and a one-time bag
  upgrade; buy buttons disable when unaffordable, out of order, or owned. Rods visibly change cast
  distance, reel speed and tension tolerance; bait changes bite rate and rarity.
- Weapon Rack next to the shop sells the knife, pistol, shotgun and ammo; owned weapons are
  equipped with 2/3/4 and the HUD shows the loaded/reserve ammo.
- Quest board with three objectives: kill 3 creatures, defeat the Spider Crab (opens Isle Two) and
  earn $500 (opens Isle Three). Completion toasts, progress rows and boat-range gating.

### Combat and survival
- A landed fish is a live `FishEnemy`: species-based HP, hop/charge movement, contact damage and a
  target HP bar. Kill it with fists, knife, pistol or shotgun; the carcass becomes a collectable
  with an E prompt. Kill bonuses: headshot +25%, airborne +50%, melee +5% value.
- Player HP (100) and food (100). Food drains slowly; at zero HP drains. F eats a bag fish to heal
  18 HP and 28 food. Death respawns at the dock with 60 HP / 50 food and costs 10% of money.
- Weapons: fists (10 damage), knife (22 melee), pistol (18, 8-round magazine, 45 m), shotgun
  (6 pellets × 13, 5 shells, 16 m). R reloads from reserve ammo; the weapon rack sells ammo packs.
- Spider Crab boss: 320 HP, telegraphed charge, self-stun window, 24 contact damage, target bar and
  a shell trophy that completes the boss quest and opens Isle Two.
- HUD: HP/food bars, weapon and ammo chip, crosshair, target name/HP bar, damage flash and a
  knocked-out overlay.

### Rowboat
- E to board/leave near the boat; WASD rowing with reverse; subtle buoyant bob and roll.
- A strong-current boundary stops the boat before locked islands and shows a hint.

### UI
- uGUI built in code with a consistent cream/teal/coral palette, rounded panels, shadows and
  readable type at 1280x720.
- HUD: money, equipped rod/bait, bag count, controls hint, interaction prompt, cast charge,
  bite alert, tension bar with safe marker, reel progress, fish stamina, toasts.
- Catch popup with species, rarity, weight, value, rare-variant line and bag status.
- Shop, quest, pause menu (resume/save/load/quit to menu/quit game), main menu (Play/Continue/Quit)
  and Continue-enabled state. The title screen hides the HUD for a clean backdrop.
- Underwater tint when the camera dips below the surface.

### Persistence and flow
- JSON save at `Application.persistentDataPath/save.json` with money, rod tier, bait, bag,
  capacity, lifetime catches/earnings, island unlocks, quest state, player pose and boat pose.
- Autosave on sell, buy, island unlock, quit and pause; F5/F9 manual save/load; Continue on the
  title screen. Pause (Esc) unlocks the cursor and freezes time; resume/save/load/quit-to-menu.

### Tooling contract
- `--screenshot <file.png>` renders ~120 frames (starting from the title screen, then entering the
  game), captures via a camera render target including the HUD, pushes the window behind others,
  runs while unfocused, saves a PNG and exits 0.
- `--self-test <file.json>` / `-selfTest` runs the scripted checks with a fixed seed, writes JSON,
  prints it, and exits 0 only if all steps pass. It covers spawn, walking 2 m, casting, forced
  bite, hook, reel, catch alive, pistol fight, collect, bag, sell, buy, save/load and HUD
  verification.
- Latest run: all 16 steps passed, no errors.

## Not implemented (out of scope / limits)
- No multiplayer, additional islands beyond the three, quest chains beyond the three island
  objectives, cooking/grill, gacha/roulette/slots, drip variants collection or extra economy.
- No music and no ambient loops; only synthesized one-shot sounds (footsteps, splashes, cast,
  nibble, bite, hook, reel click, fanfare, snap, UI/buy/error, gunshot, hit, reload, hurt, death,
  boss roar).
- No settings menu (rebindable keys, volume, resolution); resolution is fixed at a 1280x720
  window by default.
- Islands Two and Three are visible from the start but unreachable by boat until unlocked. They
  have no shop/dock content of their own; you can swim ashore and fish from them.
- No gamepad support; keyboard and mouse only.
- No hand-authored art: the visual models are the license-free CC0 packs, and animation is
  procedural (bob, sway, wave motion). Quaternius swim clips import but the held-fish display uses
  a baked swim pose rather than playing them live.
- The "nibble" stage is a cue within the waiting state rather than a separate gameplay window.
- Balance values are original estimates, not measured from another game; see MECHANICS.md.

## Technical notes
- Built-in render pipeline and built-in uGUI; dependencies are Unity built-in modules plus
  `com.unity.ugui`.
- All canvases are ScreenSpaceCamera (needed for screenshot capture) at a small plane distance so
  the held rod never draws over panels.
- Standard, Sprites/Default and UI/Default shaders are added to Always Included Shaders by the
  build script so runtime `Shader.Find` works in the player.
- The scene is generated by `CraftBuild.Build()` before each build; `Assets/Scenes/Main.unity`
  contains only the `GameBoot` component.
