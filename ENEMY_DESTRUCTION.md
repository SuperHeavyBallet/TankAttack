# Enemy explosions and persistent wrecks

When an enemy reaches zero health, it fires its death event once, awards the existing score/health-drop rewards, and becomes a wreck. Enemy AI, motor, turret aiming, and both weapons are disabled. Engine and turret loops stop, and the Rigidbody becomes immobile. The original mesh hierarchy and colliders remain active, so wrecks obstruct driving and shots. Player destruction behavior is unchanged.

Wrecks remain for the rest of the level. They are removed with the scene on extraction/restart; there is no timer or wreck-despawn limit. Further hits cannot kill them again, award more score, or drop more health. The destruction effect is visual only; weapon blast damage continues to use its existing rules.

## Explosion tuning

Open `Assets/TankAttack/Prefabs/EnemyExplosion.prefab` to edit the Unity Particle Systems:

- Root: a short flash.
- Fireball: expanding orange fire particles.
- Sparks: stretched particles with gravity.
- Smoke: larger, slowly rising particles that fade out.

The effect uses URP particle materials `ExplosionFire` and `ExplosionSmoke` and a generated soft radial texture under `Assets/TankAttack/Materials`. Particle counts, speeds, sizes, colors, lifetimes and gradients can all be edited in the normal Particle System Inspector. The effect cleans up after all child systems finish, while the wreck stays.

Each enemy prefab has **Enemy Destruction**:

- **Explosion Prefab**: replace with another Particle System hierarchy if desired. Use non-looping systems and an ExplosionLifetime component on the root for cleanup.
- **Explosion Size Multiplier**: per-enemy artistic size adjustment.
- **Reference Hull Size**: 3.6m by default. Explosion scale is the larger horizontal hull dimension divided by this value, then multiplied by the size multiplier. Root scaling is included, so scouts naturally produce smaller effects than full-size tanks.
- **Explosion Height Offset**: offsets the origin above the physical hull roof so the flash is visible.
- **Navigation Clearance**: clearance for routing tanks around a wreck. The obstacle compensates for the difference between the prototype/runtime bake radius and the registered NavMesh agent radius.

Existing destruction audio clips still play through VehicleAudio. No new audio assets are required.

## Navigation, drops, and respawns

Wrecks create carving NavMesh obstacles. AI's existing repath loop then finds routes around them. Physical collision geometry stays the same size; navigation reserves extra room for a moving tank.

Health drops search nearby ground with room for a player tank, rather than spawning inside the retained wreck. **Health Drop > Wreck Pickup Clearance** controls the offset. The search is bounded; if an area is completely crowded, the fallback still creates the reward near the wreck.

Enemy zones retain delayed respawning and random enemy selection. If the original point is blocked, the spawner searches nearby navigable, unoccupied ground, respecting minimum player distance. **Enemy Spawner > Blocked Spawn Search Radius** defaults to 12m. If no valid position exists within the radius, it waits and retries; it never deletes a wreck or spawns through it.

## Setup and checks

**Tank Attack > Set Up Enemy Destruction** creates missing effect assets and adds missing component/references on EnemyTank, LightTank, and ScoutCar. Existing effect assets and configured references are preserved on reruns. Future enemy prefabs should carry the Enemy Destruction component and reference the effect.

**Tank Attack > Run Gameplay Checks** includes wreck immobility, retained meshes and colliders, stopped weapons/audio loops, single death rewards, navigation clearance, shot blocking, accessible health drops, nearby respawn, effect scaling and effect cleanup. Preview renders are written to `Logs/TankAttack-explosions.png` and `Logs/TankAttack-wreck-smoke.png`.

Verified on 15 November 2026: 51/51 Play Mode checks passed. Explosion and smoke renders were visually reviewed. Standalone player build validation remains to be done.
