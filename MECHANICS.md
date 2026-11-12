# Castaway Cove — Mechanics Sheet

All numbers are original design choices for this game; there was no original build to measure.
Everything here is marked **estimated** because no prior version or reference data exists for
these values. Units are Unity metres and seconds.

## Controls

| Action | Input | Value |
|---|---|---|
| Move | WASD | estimated |
| Look | Mouse | sensitivity 2.2 (multiplier) — estimated |
| Sprint | Left Shift | estimated |
| Jump | Space | estimated |
| Interact / board boat | E | estimated |
| Equip rod | 1 | estimated |
| Cast / reel | Hold & release left mouse | estimated |
| Cancel fishing | Right mouse | estimated |
| Save / Load | F5 / F9 | estimated |
| Pause | Esc | estimated |

## Movement

| Value | Setting | Source |
|---|---|---|
| Walk speed | 3.6 m/s | estimated |
| Sprint speed | 6.4 m/s | estimated |
| Swim speed | 2.0 m/s | estimated |
| Acceleration | 16 m/s² | estimated |
| Air control | 0.4 (acceleration multiplier) | estimated |
| Gravity | 20 m/s² | estimated |
| Jump height | 1.1 m (jump velocity 6.6 m/s) | estimated |
| Character height / radius | 1.8 m / 0.36 m | estimated |
| Water surface | y = 0.15 m | estimated |
| Swim buoyancy | holds body centre near −0.03 m below surface | estimated |

## Camera

| Value | Setting | Source |
|---|---|---|
| Field of view | 70° | estimated |
| Pitch clamp | ±80° | estimated |
| Near / far clip | 0.06 m / 700 m | estimated |
| Eye height | 1.62 m | estimated |
| Head bob | 0.035 m walking, 0.055 m sprinting | estimated |

## Fishing

| Value | Setting | Source |
|---|---|---|
| Cast charge time | 1.15 s | estimated |
| Cast distance | 4 m (tap) to rod max (12/16/20 m) | estimated |
| Bait consumed per cast | 1 if available | estimated |
| Wait before bite | 1.6–4.2 s (×0.55 with bait) | estimated |
| Nibble cue | at 55% of the wait | estimated |
| Bite window | 1.35 s (1.7 s with bait) | estimated |
| Tension rise while reeling | 0.5 + (1 − tolerance) × 0.55 per s | estimated |
| Fish pull extra tension | +0.35/s for 0.4–0.8 s pulses | estimated |
| Tension release when slack | 0.8/s | estimated |
| Line snap | tension ≥ 1.0 | estimated |
| Reel progress rate | 0.32 × rod reel speed × (1.1 − 0.4 × fish stamina) per s | estimated |
| Slack progress loss | 0.03/s | estimated |
| Fish stamina drain / regen | 0.16/s holding / 0.03/s slack | estimated |
| Bite audio window | full bite window | estimated |

## Rods

| Tier | Name | Price | Max cast | Reel speed | Tension tolerance | Source |
|---|---|---|---|---|---|---|
| 0 | Bamboo Rod | $0 (start) | 12 m | 1.00× | 0.55 | estimated |
| 1 | Sturdy Rod | $150 | 16 m | 1.25× | 0.65 | estimated |
| 2 | Master Rod | $500 | 20 m | 1.50× | 0.75 | estimated |

## Fish table and rarity

| Species | Tier | Base value | Weight range | Table weight | Source |
|---|---|---|---|---|---|
| Sunny Sardine | common | $45 | 0.2–1.1 kg | 26 | estimated |
| Dune Minnow | common | $55 | 0.3–1.4 kg | 22 | estimated |
| Coral Perch | common | $70 | 0.6–2.6 kg | 18 | estimated |
| Lagoon Snapper | uncommon | $110 | 1.0–4.0 kg | 13 | estimated |
| Amber Tang | uncommon | $130 | 1.4–5.2 kg | 9 | estimated |
| Moonray Bass | rare | $200 | 2.0–7.5 kg | 5 | estimated |
| Stormback Eel | rare | $240 | 2.4–9.0 kg | 3 | estimated |
| Golden Leviathan | epic | $480 | 5.0–18.0 kg | 2 | estimated |

- Value = base value × random 0.8–1.5 size factor (weight is random in range independently, for flavour).
- Rare variant chance 3%, value ×5.
- With bait available on the cast: common table weight ×0.55, uncommon ×1.35, rare ×2.1, epic ×2.6.
- Start: $0, Bamboo Rod, 10 bait, bag capacity 8.

## Shop / economy

| Item | Price | Effect | Source |
|---|---|---|---|
| Sturdy Rod | $150 | cast 16 m, reel 1.25×, tolerance 0.65 | estimated |
| Master Rod | $500 | cast 20 m, reel 1.5×, tolerance 0.75 | estimated |
| Bait pack | $25 | +10 bait; better bite rate and rarity odds | estimated |
| Bag upgrade | $120 | capacity 8 → 14 (one-time) | estimated |
| Sell all fish | free | money += sum of fish values; counts toward lifetime earnings | estimated |

## Quests and progression

| Quest | Requirement | Result | Source |
|---|---|---|---|
| Catch 5 fish | lifetime catches ≥ 5 | completion message | estimated |
| Earn $100 | lifetime selling earnings ≥ $100 | unlocks Isle Two | estimated |
| Earn $500 | lifetime selling earnings ≥ $500 | unlocks Isle Three | estimated |

Boat travel limit is 46 m from the start island until Isle Two unlocks, 120 m until Isle Three
unlocks, then 260 m. Islands sit at roughly 90 m and 104 m from the start.

## Boat

| Value | Setting | Source |
|---|---|---|
| Forward speed | 7.0 m/s | estimated |
| Reverse speed | 2.6 m/s | estimated |
| Turn rate | 55°/s | estimated |
| Buoyant bob | ±0.045 m, 1.7 Hz; roll ±2.2° | estimated |
| Boarding range | 5.5 m | estimated |

## Interaction and UI

| Value | Setting | Source |
|---|---|---|
| Interaction reach | 3.6 m raycast | estimated |
| Toast lifetime | 3.4 s, 3 visible max | estimated |
| Catch card lifetime | 4.2 s | estimated |
| HUD reference resolution | 1280×720 | estimated |

## Save format

JSON at `Application.persistentDataPath/save.json`; autosaves on sell, buy, quit/pause, and on
island unlocks; manual F5/F9.
