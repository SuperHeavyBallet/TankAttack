using UnityEngine;
namespace TankAttack
{
    public sealed class TurretAim : MonoBehaviour
    {
        public VehicleSettings settings;
        public Transform yawPivot, pitchPivot;
        public Vector3 Target { get; set; }
        public float TurnAmount { get; private set; }
        void Start() => Target = transform.position + transform.forward * 20;
        void LateUpdate()
        {
            // Keep the mesh/muzzle contract, but never elevate or depress the barrel.
            pitchPivot.localRotation = Quaternion.identity;
            float yaw = yawPivot.eulerAngles.y;
            yawPivot.rotation = Quaternion.Euler(0, yaw, 0);
            TurnAmount = 0;
            if (!BattleSession.IsRunning || settings == null) return;
            Vector3 flat = Vector3.ProjectOnPlane(Target - yawPivot.position, Vector3.up);
            if (flat.sqrMagnitude < .001f) return;
            float desired = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            TurnAmount = Mathf.Clamp01(Mathf.Abs(Mathf.DeltaAngle(yaw, desired)) / 20);
            yawPivot.rotation = Quaternion.Euler(0, Mathf.MoveTowardsAngle(yaw, desired, settings.turretTurnSpeed * Time.deltaTime), 0);
        }
    }
}
