# Controller support

Gameplay accepts generic Unity Input System `Gamepad` devices, including its DualShock layout. Mouse/keyboard controls remain available.

| Control | Action |
| --- | --- |
| Left stick Y | Forward / reverse throttle |
| Left stick X | Turn the hull |
| Right stick | Aim in a screen-relative direction |
| R2 / RT | Hold to fire the cannon |
| L2 / LT | Hold to fire the machine gun |
| R1 / RB | Next collected cannon |
| L1 / LB | Previous collected cannon |
| Share / Back / Select | Restart the current level, including after death |
| Options / Start | Return to tank selection, like Escape |
| D-pad or left stick in menu | Navigate tank cards and Confirm |
| Cross / A / bottom face button | Activate the selected menu button |

Options/Start returns to the existing menu; it is not a pause/resume screen. In the selection menu, move down from a tank card to Confirm before deploying.

## Aiming and switching devices

Mouse movement or mouse/keyboard actions restore the mouse cursor reticle. Deliberate gamepad input selects directional aiming. Small stick drift does not switch modes. When switching to a controller without moving the right stick, aim begins in the turret's current direction.

Right-stick direction is relative to the camera, independent of the hull. Releasing it retains that world direction while the tank moves or turns. The turret still rotates at its configured speed and both guns remain horizontal; there is no automatic snap to an enemy. The directional reticle sits ahead of the tank, stopping sooner if a nearby surface is hit. Its display distance is capped to keep it readable, but targets along the aiming ray can be checked out to the weapon ranges. The actual muzzle paths determine cannon and machine-gun hit indicators independently.

Disconnecting the active gamepad clears driving and firing. Losing application focus also clears held controls. Reconnecting and moving a stick resumes controller input.

## Inspector tuning

On the PlayerTank prefab's **Player Tank Input** component:

- **Drive Dead Zone** (0.15): additional dead zone and analog throttle rescaling after the Input System's stick processing.
- **Aim Dead Zone** (0.2): additional threshold for changing aim direction and selecting gamepad mode.
- **Direction Reticle Distance** (25m): maximum distance of the directional aiming marker; does not change weapon range.
- **Mouse Switch Distance** (2 pixels): movement needed to switch back to cursor aiming.

Bindings live in `Assets/TankAttack/Input/TankControls.inputactions`. The existing action IDs and mouse/keyboard bindings are preserved. Menu navigation continues to use the existing Input System UI module.

## Validation scope

Automated Play Mode checks use virtual generic gamepad and DualShock-layout devices. They cover analog drive axes and dead zones, gradual aiming and retained direction, mouse switching, triggers, shoulder-button switching without bypassing reload, disconnection, independent reticle ranges and cover, menu navigation/deployment, restart after death, and returning to the menu.

These checks do not verify a physical controller's USB/Bluetooth connection, driver behavior, or actual stick feel. A physical DualShock playtest and standalone executable test are still needed. No controller vibration has been added.

Verified on 15 November 2026: all 47 Play Mode checks passed. Results: Logs/TankAttack-PlayMode.xml.
