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
        [Header("Gamepad")]
        [Range(0,.9f)] public float driveDeadZone = .15f;
        [Range(0,.9f)] public float aimDeadZone = .2f;
        [Min(3)] public float directionReticleDistance = 25;
        [Min(.1f)] public float mouseSwitchDistance = 2;
        public bool UsingGamepad { get; private set; }
        public Vector3 StickAimDirection { get; private set; } = Vector3.forward;
        public Vector2 PointerPosition { get; private set; }
        public bool HasAimPoint { get; private set; }
        public Damageable AimTarget { get; private set; }
        public bool CannonOnTarget { get; private set; }
        public bool MachineGunOnTarget { get; private set; }
        InputAction cycle, previousWeapon, nextWeapon, gamepadMove, stickAim;
        readonly InputAction[] slots = new InputAction[4];
        InputActionAsset instance;
        InputAction move, point, primary, secondary, restart;
        Camera view;
        Vector2 lastMousePosition;
        Vector2 mousePosition;
        Vector3 directionMarker;
        Gamepad activePad;

        void Awake()
        {
            instance = Instantiate(actions);
            move = instance.FindAction("Tank/Move", true); point = instance.FindAction("Tank/Point", true);
            primary = instance.FindAction("Tank/Cannon", true); secondary = instance.FindAction("Tank/MachineGun", true);
            restart = instance.FindAction("Tank/Restart", true);
            cycle = instance.FindAction("Tank/CycleWeapon");
            gamepadMove = instance.FindAction("Tank/GamepadMove", true);
            stickAim = instance.FindAction("Tank/StickAim", true);
            previousWeapon = instance.FindAction("Tank/PreviousWeapon", true);
            nextWeapon = instance.FindAction("Tank/NextWeapon", true);
            for (int i=0;i<slots.Length;i++) slots[i] = instance.FindAction("Tank/Weapon" + (i+1));
            instance.actionMaps[0].actionTriggered += OnAction;
        }
        void OnEnable()
        {
            lastMousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            mousePosition=lastMousePosition;
            instance.Enable(); Cursor.visible = false;
            InputSystem.onDeviceChange += OnDeviceChange;
        }
        void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            instance.Disable(); Cursor.visible = true; StopControls();
        }
        void OnDestroy() { if (instance != null) Destroy(instance); }
        void StopControls() { motor.Command=Vector2.zero; cannon.TriggerHeld=machineGun.TriggerHeld=false; }
        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device != activePad || (change != InputDeviceChange.Disconnected && change != InputDeviceChange.Removed && change != InputDeviceChange.Disabled)) return;
            activePad=null; UsingGamepad=false; StopControls();
        }
        void OnAction(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            if (context.action == point)
            {
                mousePosition=context.ReadValue<Vector2>();
                if ((mousePosition-lastMousePosition).sqrMagnitude >= mouseSwitchDistance*mouseSwitchDistance)
                { UsingGamepad=false; lastMousePosition=mousePosition; }
                return;
            }
            if (context.control.device is Gamepad pad)
            {
                if (context.action == gamepadMove && context.ReadValue<Vector2>().magnitude <= driveDeadZone) return;
                if (context.action == stickAim && context.ReadValue<Vector2>().magnitude <= aimDeadZone) return;
                if (!UsingGamepad)
                {
                    StickAimDirection=Vector3.ProjectOnPlane(aim.yawPivot.forward,Vector3.up).normalized;
                    if (StickAimDirection.sqrMagnitude < .01f) StickAimDirection=Vector3.forward;
                }
                activePad=pad; UsingGamepad=true;
            }
            else if (context.control.device is Keyboard || context.control.device is Mouse) UsingGamepad=false;
        }
        void Update()
        {
            if (!Application.isFocused) { StopControls(); return; }
            ReadControls();
        }
        // Separating sampling from the focus gate also allows deterministic virtual-device checks.
        public void ReadControls()
        {
            if (restart.WasPressedThisFrame()) { BattleSession.Instance?.Restart(); return; }
            if (!BattleSession.IsRunning) { StopControls(); return; }
            if (view == null) view=Camera.main;
            Vector2 mouse=mousePosition;
            if (UsingGamepad) ResolveStickAim(stickAim.ReadValue<Vector2>());
            else
            {
                PointerPosition=mouse;
                if (view != null) ResolveAim(view.ScreenPointToRay(mouse));
                else { HasAimPoint=false; AimTarget=null; }
            }
            for (int i=0;i<slots.Length;i++) if (slots[i] != null && slots[i].WasPressedThisFrame()) cannon.Select(i);
            float scroll=cycle != null ? cycle.ReadValue<float>() : 0;
            if (scroll != 0) cannon.Cycle(scroll > 0 ? 1 : -1);
            if (nextWeapon.WasPressedThisFrame()) cannon.Cycle(1);
            else if (previousWeapon.WasPressedThisFrame()) cannon.Cycle(-1);
            motor.Command=UsingGamepad ? DeadZone(gamepadMove.ReadValue<Vector2>(),driveDeadZone) : move.ReadValue<Vector2>();
            cannon.TriggerHeld=HasAimPoint && primary.IsPressed();
            machineGun.TriggerHeld=HasAimPoint && secondary.IsPressed();
            GetComponent<VehicleAudio>()?.SetMotion(motor.Command.magnitude,aim.TurnAmount);
        }
        static Vector2 DeadZone(Vector2 value,float threshold)
        {
            float magnitude=value.magnitude;
            return magnitude<=threshold ? Vector2.zero : value.normalized*Mathf.Clamp01((magnitude-threshold)/(1-threshold));
        }
        void ResolveStickAim(Vector2 stick)
        {
            if (stick.magnitude > aimDeadZone)
            {
                Vector3 right=view != null ? Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized : Vector3.right;
                Vector3 forward=Vector3.Cross(right,Vector3.up);
                StickAimDirection=(right*stick.x+forward*stick.y).normalized;
            }
            Vector3 origin=aim.pitchPivot.position;
            float range=Mathf.Max(cannon.settings.range,machineGun.settings.range);
            bool hitSomething=CombatPhysics.Cast(origin,StickAimDirection,range,0,transform,out var hit);
            AimTarget=hitSomething ? hit.collider.GetComponentInParent<Damageable>() : null;
            float distance=hitSomething ? hit.distance : directionReticleDistance;
            // Keep the requested direction exact: there is no automatic snap toward a target's centre.
            aim.Target=origin+StickAimDirection*Mathf.Max(.01f,distance);
            directionMarker=origin+StickAimDirection*Mathf.Min(directionReticleDistance,distance);
            HasAimPoint=true;
        }
        public void ResolveAim(Ray ray)
        {
            HasAimPoint = CombatPhysics.Cast(ray.origin,ray.direction,400,0,transform,out var hit,includeShotVolumes: false);
            AimTarget = HasAimPoint ? hit.collider.GetComponentInParent<Damageable>() : null;
            if (!HasAimPoint) return;
            aim.Target = AimTarget != null ? hit.collider.bounds.center : hit.point;
        }
        void LateUpdate()
        {
            if (UsingGamepad && view != null)
            {
                Vector3 screen=view.WorldToScreenPoint(directionMarker);
                PointerPosition=new Vector2(Mathf.Clamp(screen.x,18,Screen.width-18),Mathf.Clamp(screen.y,18,Screen.height-18));
            }
            RefreshReticle();
        }
        public void RefreshReticle()
        {
            Transform target = AimTarget != null && !AimTarget.IsDead ? AimTarget.transform : null;
            CannonOnTarget = HasAimPoint && cannon.CanHit(aim.Target,target);
            MachineGunOnTarget = HasAimPoint && machineGun.CanHit(aim.Target,target);
        }
        void OnApplicationFocus(bool focused)
        {
            if (!focused) StopControls();
            lastMousePosition=Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            mousePosition=lastMousePosition;
            Cursor.visible = !focused || !enabled || !BattleSession.IsRunning;
        }
    }
}
