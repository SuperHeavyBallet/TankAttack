using UnityEngine;
using UnityEngine.InputSystem;
namespace TankAttack
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerTankInput : MonoBehaviour
    {
        public InputActionAsset actions;
        public TankMotor motor;
        public TurretAim aim;
        public TankWeapon cannon, machineGun;
        public Vector2 PointerPosition { get; private set; }
        public bool HasAimPoint { get; private set; }
        public Damageable AimTarget { get; private set; }
        public bool CannonOnTarget { get; private set; }
        public bool MachineGunOnTarget { get; private set; }
        InputAction cycle;
        readonly InputAction[] slots = new InputAction[4];
        InputActionAsset instance;
        InputAction move, point, primary, secondary, restart;
        Camera view;
        void Awake()
        {
            instance = Instantiate(actions);
            move = instance.FindAction("Tank/Move", true); point = instance.FindAction("Tank/Point", true);
            primary = instance.FindAction("Tank/Cannon", true); secondary = instance.FindAction("Tank/MachineGun", true);
            restart = instance.FindAction("Tank/Restart", true);
            cycle = instance.FindAction("Tank/CycleWeapon");
            for (int i=0;i<slots.Length;i++) slots[i] = instance.FindAction("Tank/Weapon" + (i+1));
        }
        void OnEnable() { instance.Enable(); Cursor.visible = false; }
        void OnDisable()
        {
            instance.Disable(); Cursor.visible = true; motor.Command = Vector2.zero;
            cannon.TriggerHeld = machineGun.TriggerHeld = false;
        }
        void OnDestroy() { if (instance != null) Destroy(instance); }
        void Update()
        {
            if (restart.WasPressedThisFrame()) { BattleSession.Instance?.Restart(); return; }
            if (!BattleSession.IsRunning || !Application.isFocused) { motor.Command = Vector2.zero; cannon.TriggerHeld = machineGun.TriggerHeld = false; return; }
            if (view == null) view = Camera.main;
            PointerPosition = point.ReadValue<Vector2>();
            if (view != null) ResolveAim(view.ScreenPointToRay(PointerPosition));
            else { HasAimPoint = false; AimTarget = null; }
            for (int i=0;i<slots.Length;i++) if (slots[i] != null && slots[i].WasPressedThisFrame()) cannon.Select(i);
            float scroll = cycle != null ? cycle.ReadValue<float>() : 0;
            if (scroll != 0) cannon.Cycle(scroll > 0 ? 1 : -1);
            motor.Command = move.ReadValue<Vector2>();
            cannon.TriggerHeld = HasAimPoint && primary.IsPressed();
            machineGun.TriggerHeld = HasAimPoint && secondary.IsPressed();
            GetComponent<VehicleAudio>()?.SetMotion(motor.Command.magnitude, aim.TurnAmount);
        }
        public void ResolveAim(Ray ray)
        {
            HasAimPoint = CombatPhysics.Cast(ray.origin,ray.direction,400,0,transform,out var hit);
            AimTarget = HasAimPoint ? hit.collider.GetComponentInParent<Damageable>() : null;
            if (!HasAimPoint) return;
            // Aim into the target volume rather than grazing the camera-visible roof.
            aim.Target = AimTarget != null ? hit.collider.bounds.center : hit.point;
        }
        void LateUpdate() => RefreshReticle();
        public void RefreshReticle()
        {
            Transform target = AimTarget != null && !AimTarget.IsDead ? AimTarget.transform : null;
            CannonOnTarget = HasAimPoint && cannon.CanHit(aim.Target,target);
            MachineGunOnTarget = HasAimPoint && machineGun.CanHit(aim.Target,target);
        }
        void OnApplicationFocus(bool focused) => Cursor.visible = !focused || !enabled || !BattleSession.IsRunning;
    }
}
