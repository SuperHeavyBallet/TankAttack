# Tank Attack: tank selection and engine audio

## Installation

Installed and verified on 12 November 2026: all 31 Unity Play Mode checks passed, including all three tank deployments, restart/base-weapon behavior, persistent audio ownership, loop overlap and fade-out. The menu render was visually inspected. Existing tuning assets were verified byte-for-byte unchanged. The steps below are for applying the update to another checkout; this project is already installed.

1. Leave Play mode and save the current Unity scene.
2. Extract this archive into the project root (E:/SHB/Tank Attack), merging Assets with the existing Assets folder. The archive contains source changes; it does not replace existing tuning assets, .meta files, or scenes. Replace the included source files when prompted.
3. In Unity, choose Assets > Refresh and wait for compilation.
4. Choose Tank Attack > Install Tank Selection and Engine Audio. This creates the new menu, tank definitions and audio settings, adds the player configuration component, and makes the menu the first build scene. Existing tuning assets and the arena geometry are preserved.
5. Choose Tank Attack > Run Gameplay Checks. Results are written to Logs/TankAttack-PlayMode.xml. A menu image is written to Logs/TankAttack-menu.png.
6. Choose Tank Attack > Open Main Menu, then press Play.

Menu setup is additive: assets and menu content already present are preserved on later runs. The gameplay scene can still be played directly for development; it uses Tank 2 when no menu selection has been made. Built games start in the menu.

## Tank selection

Click a tank card to see its actual tuning values, then Confirm & Deploy. Keyboard arrows move between cards; Enter activates the selected button. Down moves to Confirm. Escape in gameplay returns to the menu; R restarts with the same selected tank.

Tank 2 references the current player vehicle and base cannon settings directly. Tank 1 and Tank 3 get independent copies derived once from that current baseline:

| Tank | Max health | Forward/reverse speed | Hull turn | Acceleration | Base cannon damage |
|---|---:|---:|---:|---:|---:|
| Light | 70% | 135% | 150% | 145% | 65% |
| Medium | Current values | Current values | Current values | Current values | Current values |
| Heavy | 180% | 65% | 65% | 65% | 180% |

These are starting values, not ongoing multipliers. Edit LightPlayerVehicle, HeavyPlayerVehicle, LightPlayerCannon and HeavyPlayerCannon in Assets/TankAttack/Settings. Tank definitions are LightPlayerTank, MediumPlayerTank and HeavyPlayerTank. PlayerTankRoster holds the three definitions and scene paths. The current greybox visual is shared; tank art can be developed separately.

A selected tank's health, handling, turret and base cannon initialize before gameplay components start. An emptied weapon pickup falls back to that tank's own base cannon. Selection persists across scene changes and restarts in the same session, not across application launches.

## Engine rumble

Select Assets/TankAttack/Settings/PlayerEngineAudio.asset and assign Rumble Clip. Adjust Volume, Idle Volume, Overlap Seconds, Fade In Seconds and Fade Out Seconds. An optional AudioMixerGroup routes the sound into a mixer. Every tank definition references this asset by default; separate engine profiles can be assigned per tank later.

The player uses one DontDestroyOnLoad audio service with two AudioSources. DSP-scheduled clip starts overlap; equal-power fades blend the outgoing and incoming copies. Start/stop also fade. The service is reused across restarts and fades out on death, victory or return to the menu. A missing clip remains silent. If no profile clip is assigned, the existing VehicleAudio.engineLoop field remains a fallback.

Use a reasonably steady engine-rumble recording with Preload Audio Data enabled; Decompress On Load is a suitable starting point for a short loop. Overlap is clamped below half the clip length. The player loop uses fixed pitch to maintain scheduled timing; volume rises with driving input. Existing enemy-local loops and weapon/impact sounds retain their prior behavior.

No production rumble clip is included. The audio test uses a very quiet generated tone to check overlap and release; the final clip still needs listening and tuning in Unity.

## Status and limitations

- C# compilation: verified for runtime, editor and test sources.
- Unity scene generation and 31 Play Mode checks: passed. Menu visual review: completed. Audio overlap/fades verified with a generated quiet tone; the production rumble clip still needs to be assigned and auditioned.
- Existing tuning assets: verified unchanged. The gameplay scene received the selected-tank startup component; existing arena geometry was retained.
- Standalone build: not verified.
