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
            if (!BattleSession.IsRunning) return;
            Vector3 delta = Target - pitchPivot.position;
            Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
            if (flat.sqrMagnitude < .001f) return;
            Quaternion desired = Quaternion.LookRotation(flat, Vector3.up);
            TurnAmount = Mathf.Clamp01(Quaternion.Angle(yawPivot.rotation, desired) / 20);
            yawPivot.rotation = Quaternion.RotateTowards(yawPivot.rotation, desired, settings.turretTurnSpeed * Time.deltaTime);
            float elevation = -Mathf.Atan2(delta.y, flat.magnitude) * Mathf.Rad2Deg;
            var pitch = Quaternion.Euler(Mathf.Clamp(elevation, -35, 40), 0, 0);
            pitchPivot.localRotation = Quaternion.RotateTowards(pitchPivot.localRotation, pitch, settings.elevationSpeed * Time.deltaTime);
        }
    }
}
