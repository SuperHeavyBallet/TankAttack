# Tank Attack development conventions

- This is a Unity 6.5 (6000.5.9f1) project using URP 17.5 and Input System 1.20. Use the versions in Packages/manifest.json and ProjectSettings/ProjectVersion.txt as the source of truth.
- Read TANK_ATTACK.md for the current playable scope, controls, asset contracts, and validation workflow.
- Keep gameplay source under Assets/TankAttack. Keep the original SampleScene intact.
- Use the Input System, never UnityEngine.Input. Expose designer tuning through settings assets and serialized fields.
- Player input and AI share TankMotor, TurretAim, TankWeapon, and Damageable. Avoid building speculative frameworks for future enemy types.
- Preserve designer changes to scenes, prefabs, settings, and art. The scene generator only creates a missing prototype; it opens an existing prototype without regenerating it.
- Replace visual mesh children without deleting HullVisual, TurretYaw, GunPitch, or muzzle transforms. Forward is local +Z, up is +Y; use meters and unit root scale.
- Commit Unity .meta files with their assets. Exclude Library, Temp, Logs, generated projects, and build output from version control.
- Use Tank Attack > Run Gameplay Checks for gameplay changes. Results are in Logs/TankAttack-PlayMode.xml. A file at Temp/TankAttack.RunChecks.request queues the same command in an open, idle editor after scripts import. Do not trigger it while the user is editing an unsaved scene.
- Never claim tests ran based on compilation alone. Report any blocked editor/build verification explicitly.

- Weapon capacity exactly 101 means unlimited, by user request; other nonnegative values are actual starting ammo. Preserve ammo per collected weapon and retain reload timers on switching.
- The main reticle indicates the cannon; its center dot indicates the machine gun. Enemy aim validation uses actual first contact with the selected target volume, not agreement between camera and muzzle hit points.
- Enemy friendly fire deals normal damage. Friendly kills must not award player score. Health drops, delayed respawn, and blast occlusion must continue to work.
- PrototypeExpansion is an additive, one-time content migration; preserve existing scene/prefab/tuning edits on reruns.
- When the editor has not imported code changes, write Refresh to Temp/TankAttack.RunChecks.request first. Wait for the project assemblies to rebuild before queuing tests or scene changes; this avoids asking the user to refresh repeatedly.
- Menu: Tank Attack > Open Main Menu. TankMenuBuilder is additive and must not reset existing tuning assets. Tank 2 references current player settings; light/heavy variants have separate assets. PlayerTankConfiguration must run before health, weapon and audio Awake initialization. EngineAudioSettings controls the persistent two-source player rumble; no clip should remain silent.
