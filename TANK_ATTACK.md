# Tank Attack — pickups and light enemies

## Open and play

In Unity, choose **Tank Attack > Open Main Menu**, then press Play. Select a tank, review its stats, then Confirm & Deploy. Builds start in the menu. The gameplay scene can still be opened directly for development and defaults to Tank 2.
Default gameplay scene: Assets/TankAttack/Scenes/TankAttack_Generated.unity. The original hand-built TankAttack_Prototype scene remains available as a regression sandbox.

- W/S or up/down: forward/reverse.
- A/D or left/right: rotate the hull, including while stationary.
- Mouse: aim the independently rotating turret; both guns remain horizontal.
- Hold left mouse: fire cannon shells on the reload interval.
- Hold right mouse: fire the coaxial machine gun.
- Drive over a weapon pickup to collect and equip it.
- Keys 1–4 select collected cannons (basic cannon first, then collection order); mouse wheel cycles them.
- R: restart with the selected tank, including after victory or destruction.
- Escape: return to tank selection.
- Reach the blue extraction zone at the far end of the arena. Destroy enemy tanks for points.

The main red/green crosshair reports the **equipped cannon**; the small center dot reports the machine gun independently. Both are calculated once per frame after turret rotation. Selecting an enemy aims into its collider and accepts a first hit anywhere on that enemy, rather than comparing the camera's roof contact with the barrel's front-armour contact. Solid cover still blocks the signal immediately. Reload is displayed separately; a travelling shell can still miss an enemy that moves after firing.

Generated levels have two spawn zones per complete 50 meters of length (minimum two). Each zone picks one random heavy tank, light tank, or scout car, including on respawn. The original prototype still uses its existing spawn pattern. Slots refill eight seconds after destruction and wait if occupied or too near the player. Enemies damage other enemies with their normal weapon damage, including stray shells and blasts. The shooter is excluded from its own shot/blast. Enemy-on-enemy kills drop health and respawn but award no player score.

## Health pickups

Destroying an enemy drops a green health crate. Heavy tanks restore 25, light tanks 15, and scout cars 8 health by default. Change **HealthDrop > Heal Amount** on the corresponding enemy prefab. **Pickup Prefab** selects the health pickup appearance/behavior.

Drive over a crate to collect it. Healing is capped at maximum health. A full-health player leaves the crate available; enemies cannot collect it. The HealthPickup prefab exposes **Lifetime** (90 seconds by default; zero means no expiry) and a default Amount, overridden by the enemy's Heal Amount on a death drop.

## Cannon pickups and ammo

Three labeled weapon crates are placed near the starting area:

| Asset | Default behavior |
| --- | --- |
| LongRangeCannon | Range 140 meters, compared with the basic cannon's 80 |
| HeavyCannon | 90 damage, 1.8-second firing interval |
| BlastCannon | 35 damage, 6-meter blast radius, 1.7-second firing interval |

Tune these in Assets/TankAttack/Settings. **Capacity = exactly 101 means unlimited ammo.** Every other nonnegative value is the starting ammo count: 5 means five shots, 200 means two hundred. Zero cannot be equipped. Every successful discharge consumes one round, including a shot that hits a wall; pressing fire during reload consumes none. Finite weapons automatically return to the basic cannon when empty. Switching retains each weapon's ammo and never bypasses reload. Collecting another copy refills that weapon to its configured starting capacity. Settings changes affect ammo on the next collection or new run.

Blast Radius = 0 means direct damage only. Positive values apply the weapon's damage to other units within the radius with a clear blast path. Walls block splash, multiple colliders do not multiply damage, and the direct target is not damaged twice. The expanding ring is a placeholder blast effect. Generated-level extraction carries health, collected weapons, per-weapon ammo, equipped cannon, remaining reload time, and score into the next level. Restart regenerates the same layout and restores that level's entry health/loadout/score. A new deployment resets the run. The hand-built prototype retains its original restart behavior.

## Designer tuning

Assets/TankAttack/Settings contains separate player/enemy vehicle, cannon, and machine-gun assets. Tune speeds, acceleration, braking, turn rates, turret turn rates, health, weapon damage, range, cadence, and projectile speed there. Scene objects expose camera offset, spawn locations, respawn delay, and destination bounds.

## Art and audio contracts

Prefabs: Assets/TankAttack/Prefabs/PlayerTank.prefab and EnemyTank.prefab.

Replace the meshes below HullVisual and TurretYaw. Keep the hierarchy pivots and existing component references intact. GunPitch carries the weapon meshes and CannonMuzzle/MachineGunMuzzle transforms. Local +Z is forward, +Y is up; distances are meters. Keep the Rigidbody and gameplay collider on the root. Adjust the collider and navigation clearance if the tank footprint changes. Cosmetic meshes have no colliders, so the prototype hull box defines movement collisions. Damageable creates a shot-only trigger with the same footprint and a minimum world height of 3 meters (Shot Target Height in the Inspector), allowing level guns to hit smaller vehicles. This does not add physical contact or block driving; camera selection still uses the original hull collider.

WeaponSettings exposes fireClip, impactClip, volume, and a referenced tracer material. VehicleAudio on each tank exposes engineLoop, turretLoop, destructionClip, and an optional AudioMixerGroup. Assign clips before entering Play mode; unassigned slots remain silent. These are ordinary serialized asset references, so no Resources directory or runtime filename conventions are required. Spatial impact sounds currently use the default audio output; vehicle sounds use the assigned mixer group.

## Architecture

- PlayerTankInput owns a private instance of TankControls.inputactions and caches separate cannon/MG reticle results after aiming.
- TankMotor handles upright Rigidbody movement and yaw steering; TurretAim handles horizontal yaw only. GunPitch stays at identity; the legacy elevation speed setting is unused.
- TankWeapon handles cadence, collected cannon selection, per-weapon ammo, and firing. ShellProjectile performs swept collision for the cannon. CombatPhysics filters owner colliders and resolves hits.
- Damageable owns health, capped healing, and a one-time death event. HealthDrop spawns HealthPickup on enemy death; WeaponPickup equips/refills cannon variants. BattleSession owns score and victory/defeat.
- EnemyTankAI follows NavMesh paths and operates the shared vehicle components. EnemySpawner owns delayed spawn slots and their repeating enemy-variant pattern.
- FollowCamera and the temporary IMGUI HUD supply the top-down view and prototype feedback.

## Editing the arena

Keep solid scenery below the Environment object and give it colliders. After moving ground or obstacles, run **Tank Attack > Rebake Prototype Navigation**, then save the scene. This bakes a 2.3-meter agent radius for the tank footprint. The generator preserves an existing prototype scene. It does not repeatedly overwrite designer edits.

## Verification

Use **Tank Attack > Apply Pickups and Light Enemies** once when upgrading an older prototype scene. It preserves the existing layout and skips content already installed.

Use **Tank Attack > Run Gameplay Checks**. The command opens the prototype and enters Play mode to run the project tests. Save your scene first. Results are saved to Logs/TankAttack-PlayMode.xml, and a camera render is saved to Logs/TankAttack-preview.png. You can also run TankAttack.PlayModeTests through Unity's Test Runner.

Checks cover movement/reversing/turning, solid-cover collision, finite turret aiming and blocked aim, travelling cannon damage/reload, hitscan damage, both weapons being blocked by cover, delayed respawning and one-time scoring, complete navigation paths, enemy attacks, destination completion, and death/friendly-fire behavior.

## Current limits and next design pass

This milestone contains heavy tanks, lighter tanks, and wheeled scout cars. Infantry and helicopters remain future work. AI uses basic path following without tactical cover selection or sophisticated vehicle avoidance. There is no armour model, saving, or finished audio/VFX. The HUD is intentionally temporary. The prototype has not yet been validated as a standalone player build.

First playtest priorities: hull responsiveness, turret speed, cannon cadence, camera framing, machine-gun usefulness, and the difficulty of reaching extraction under respawn pressure.

## Recorded validation

Initial prototype: 10 Play Mode checks passed. Expansion verification is recorded in Logs/TankAttack-PlayMode.xml. Standalone build validation remains outstanding.


## Tank selection and persistent engine audio

See MENU_AUDIO_SETUP.md for the three player tank definitions, separate light/heavy tuning, and the persistent two-source crossfade utility. Assign the engine rumble clip in Assets/TankAttack/Settings/PlayerEngineAudio.asset. The medium tank references the existing player settings directly. Verified on 12 November 2026: 31 Play Mode checks passed, menu render reviewed, and all pre-existing tuning assets remained byte-for-byte unchanged.


## Horizontal aiming and upright hulls

TankMotor freezes physics rotation on all three axes at runtime and supplies yaw through Rigidbody.MoveRotation. This is intentional: steering still works, while contact impulses cannot roll, pitch, or spin the hull. Position remains physically simulated. TurretAim keeps GunPitch level even when the cursor is close to the tank or above it. Ground reticle alignment uses horizontal distance at muzzle height; selected enemies still require a first-contact hit and cover blocks shots.

Verified on 12 November 2026: all 34 Play Mode checks passed (Logs/TankAttack-PlayMode.xml), including an off-centre Rigidbody impact while steering against cover, level guns at close/elevated targets, and horizontal shots against scouts. Direct scripted torque is outside the collision regression; external scripts should not apply rotation/torque to TankMotor bodies. No standalone build was run for this change.

## Generated level progression

The main menu now deploys into randomly generated levels. See GENERATED_LEVELS.md for inspector tuning, per-band populations, navigation clearance, and persistent run state. Defaults: length 50-75m on level one, upper bound grows 25m per level up to 300m; width 50-200m capped by length. Each complete 50m band adds two enemy zones and three weapon pickups. Reaching extraction automatically advances the run.

Verified on 15 November 2026: all 39 Play Mode checks passed, including maximum-map navigation and extraction/state carry-over. Generated map overview reviewed. Original prototype scene and pre-existing tank/weapon tuning were preserved. Standalone build verification remains outstanding.

## Controller support

Generic gamepad controls are available alongside mouse/keyboard. Left stick drives and turns, right stick aims directionally, RT/R2 fires the cannon, LT/L2 fires the machine gun, and shoulders cycle collected weapons. Releasing the right stick retains aim direction. Mouse movement/clicks restore cursor aiming. Options/Start returns to selection; Share/Back restarts, including after death. Menu navigation uses D-pad/left stick and the bottom face button to activate a selection.

See CONTROLLER_SUPPORT.md for bindings, inspector dead zones, reticle behavior, and limitations. Verified on 15 November 2026: 47/47 Play Mode checks passed using virtual generic gamepad and DualShock-layout devices. Physical controller and standalone executable tests have not yet been done.

## Enemy explosions and wrecks

Destroyed enemies play a size-scaled Particle System explosion and retain their original meshes as immobile, solid wrecks until the level unloads. AI, turret, weapons and engine/turret loops stop. Wrecks block movement and shots and carve navigation with tank clearance. Health drops move to nearby clear ground; blocked spawn zones search nearby navigable positions without deleting wrecks. Death events, score and drops remain one-time, including enemy friendly-fire kills.

See ENEMY_DESTRUCTION.md for effect prefab/Inspector tuning and spawn/drop behavior. Verified on 15 November 2026: all 51 Play Mode checks passed and explosion/smoke previews were visually reviewed. The suite also exposed intermittent shared menu-input state; the menu now owns a private copy of its UI input actions. Standalone build verification has not been done.
