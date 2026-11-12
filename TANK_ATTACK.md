# Tank Attack — pickups and light enemies

## Open and play

In Unity, choose **Tank Attack > Create or Open Prototype**, then press Play.
Scene: Assets/TankAttack/Scenes/TankAttack_Prototype.unity.

- W/S or up/down: forward/reverse.
- A/D or left/right: rotate the hull, including while stationary.
- Mouse: aim the independently rotating turret and elevating barrel.
- Hold left mouse: fire cannon shells on the reload interval.
- Hold right mouse: fire the coaxial machine gun.
- Drive over a weapon pickup to collect and equip it.
- Keys 1–4 select collected cannons (basic cannon first, then collection order); mouse wheel cycles them.
- R: restart, including after victory or destruction.
- Reach the blue extraction zone at the far end of the arena. Destroy enemy tanks for points.

The main red/green crosshair reports the **equipped cannon**; the small center dot reports the machine gun independently. Both are calculated once per frame after turret rotation. Selecting an enemy aims into its collider and accepts a first hit anywhere on that enemy, rather than comparing the camera's roof contact with the barrel's front-armour contact. Solid cover still blocks the signal immediately. Reload is displayed separately; a travelling shell can still miss an enemy that moves after firing.

The six spawn slots cycle heavy tanks, light tanks, and scout cars. Slots refill eight seconds after destruction and wait if occupied or too near the player. Enemies damage other enemies with their normal weapon damage, including stray shells and blasts. The shooter is excluded from its own shot/blast. Enemy-on-enemy kills drop health and respawn but award no player score.

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

Blast Radius = 0 means direct damage only. Positive values apply the weapon's damage to other units within the radius with a clear blast path. Walls block splash, multiple colliders do not multiply damage, and the direct target is not damaged twice. The expanding ring is a placeholder blast effect. Pickups persist only for the current run; restart restores the level and basic loadout.

## Designer tuning

Assets/TankAttack/Settings contains separate player/enemy vehicle, cannon, and machine-gun assets. Tune speeds, acceleration, braking, turn rates, turret/elevation rates, health, weapon damage, range, cadence, and projectile speed there. Scene objects expose camera offset, spawn locations, respawn delay, and destination bounds.

## Art and audio contracts

Prefabs: Assets/TankAttack/Prefabs/PlayerTank.prefab and EnemyTank.prefab.

Replace the meshes below HullVisual and TurretYaw. Keep the hierarchy pivots and existing component references intact. GunPitch carries the weapon meshes and CannonMuzzle/MachineGunMuzzle transforms. Local +Z is forward, +Y is up; distances are meters. Keep the Rigidbody and gameplay collider on the root. Adjust the collider and navigation clearance if the tank footprint changes. Cosmetic meshes have no colliders, so the prototype hull box defines the complete target hitbox.

WeaponSettings exposes fireClip, impactClip, volume, and a referenced tracer material. VehicleAudio on each tank exposes engineLoop, turretLoop, destructionClip, and an optional AudioMixerGroup. Assign clips before entering Play mode; unassigned slots remain silent. These are ordinary serialized asset references, so no Resources directory or runtime filename conventions are required. Spatial impact sounds currently use the default audio output; vehicle sounds use the assigned mixer group.

## Architecture

- PlayerTankInput owns a private instance of TankControls.inputactions and caches separate cannon/MG reticle results after aiming.
- TankMotor handles Rigidbody movement; TurretAim handles yaw and elevation.
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

