# Generated levels

Open **Tank Attack > Open Main Menu**, press Play, choose a tank, and confirm. The roster now loads `Assets/TankAttack/Scenes/TankAttack_Generated.unity`. Extraction automatically loads the next generated level.

## Inspector tuning

Select `Assets/TankAttack/Settings/LevelGeneration.asset`:

| Setting | Default |
| --- | --- |
| Minimum / maximum length | 50 / 300 meters |
| First-level maximum length | 75 meters |
| Length growth per level | 25 meters |
| Minimum / maximum width | 50 / 200 meters |
| Preferred width relative to length | 0.65 |
| Band length | 50 meters |
| Terrain blocks attempted per band | 4 |
| Minimum terrain clearance | 6 meters |
| Clear central route width | 8 meters |
| NavMesh agent radius | 2.3 meters |
| Clear start/end areas | 10 meters |
| Fixed seed | 0 (fresh seed on deployment) |

The length upper bound rises by the growth amount each level, capped at Maximum Length. The lower bound rises by half that amount, so later levels also become more consistently long. With defaults, level 1 is 50–75m, level 2 is 62.5–100m, and sufficiently late levels are 300m. Width is randomly chosen within its limits and preferred ratio, never greater than length. The 50m minimum width makes the shortest maps square.

Each complete length band gets two opposing spawn zones and one of each configured weapon pickup. The minimum is one band: 50m has two zones/three pickups; 300m has twelve zones/eighteen pickups. A remainder shorter than 50m does not add another pair. Width affects placement and spacing, not the number of zones. Enemies retain delayed respawning, with a fresh random enemy choice on each respawn.

Terrain Prefabs contains independent greybox copies of the prototype's cover blocks, reduced to fit the smallest maps. Replace these references with your own prefabs. Their pivots may vary: the generator measures solid child colliders, grounds their bounds, rotates them in 90-degree steps, and checks their world footprints. Use static solid colliders and avoid Rigidbody/animated terrain. Weapon Pickups references the existing three pickup prefabs, preserving their configured weapons and visuals.

Terrain placement reserves the central route, wall clearance, start/extraction areas, spawn footprints, and pickup access. If a block cannot fit after the configured attempts it is skipped, so terrain density is a target rather than a guaranteed count. Clearance is never allowed below twice the navigation radius plus a small margin. The NavMesh includes only generated static geometry; tanks, shot triggers, pickups, and extraction markers are excluded. All spawn zones, pickups, and extraction must have a complete NavMesh route from the player before gameplay starts.

## Run behavior

The player is held still and combat is paused while geometry and navigation are built. Enemy spawning begins only after generation and route validation succeed. A generation failure leaves a loading/error message rather than starting an unplayable level.

Extraction carries the chosen tank, remaining health, collected weapons, ammo, equipped weapon, remaining reload time, and accumulated score into the next level. Repeated extraction contacts cannot advance more than once. R restarts the same level seed and its entry state; Esc returns to tank selection. Confirming deployment begins a fresh run. There is no disk save yet.

## Existing content and setup

The menu, tank definitions, audio, shared movement/aiming/combat, health drops, friendly fire, and delayed enemy respawn remain in use. The original prototype and SampleScene are unchanged. The generated scene was copied from the current prototype to retain player, camera, lighting, extraction, and spawner tuning, then its static arena/pickups were replaced with a runtime generator.

**Tank Attack > Set Up Generated Levels** creates the scene/settings/prefabs only when missing. Rerunning opens the generated scene and ensures build registration; it does not regenerate an existing arena or overwrite tuning. Custom changes made later to the prototype do not automatically propagate to the separate generated scene. Tank and weapon assets shared between scenes still update both.

## Validation

Use **Tank Attack > Run Gameplay Checks** with saved scenes. In addition to the existing gameplay checks, tests cover early and maximum dimensions, per-band content counts, terrain clearance, navigation at 50×50 and 300×200, pickups/spawns connected to extraction, random enemy spawning and respawn, extraction transitions, carry-over state, and repeatable restarts. The test run writes `Logs/TankAttack-PlayMode.xml` and a maximum-map overview at `Logs/TankAttack-generated.png`.

Navigation builds synchronously during the loading phase. A standalone player build and long-session performance profiling remain separate validation work.

Verified on 15 November 2026: 39/39 Play Mode checks passed. The 300x200m overview was visually reviewed. Original prototype and pre-existing vehicle/weapon tuning remained unchanged.

