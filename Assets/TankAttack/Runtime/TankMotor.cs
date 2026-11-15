using UnityEngine;
namespace TankAttack
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TankMotor : MonoBehaviour
    {
        public VehicleSettings settings;
        public Vector2 Command { get; set; }
        Rigidbody body;
        float speed;
        void Awake()
        {
            body = GetComponent<Rigidbody>();
            // Rotation constraints use the inertia frame. Align it with the upright hull.
            body.inertiaTensorRotation = Quaternion.identity;
            // Physics cannot add rotation; MoveRotation supplies the commanded yaw.
            body.constraints |= RigidbodyConstraints.FreezeRotation;
            body.rotation = Quaternion.Euler(0, body.rotation.eulerAngles.y, 0);
        }
        void FixedUpdate()
        {
            if (settings == null || body.isKinematic) return;
            var input = BattleSession.IsRunning ? new Vector2(Mathf.Clamp(Command.x, -1, 1), Mathf.Clamp(Command.y, -1, 1)) : Vector2.zero;
            float desired = input.y * (input.y >= 0 ? settings.forwardSpeed : settings.reverseSpeed);
            float rate = Mathf.Abs(desired) < Mathf.Abs(speed) || desired * speed < 0 ? settings.braking : settings.acceleration;
            speed = Mathf.MoveTowards(speed, desired, rate * Time.fixedDeltaTime);
            body.angularVelocity = Vector3.zero;
            Quaternion heading = Quaternion.Euler(0, body.rotation.eulerAngles.y + input.x * settings.hullTurnSpeed * Time.fixedDeltaTime, 0);
            body.MoveRotation(heading);
            Vector3 velocity = heading * Vector3.forward * speed;
            velocity.y = body.linearVelocity.y;
            body.linearVelocity = velocity;
        }
        void OnDisable() { Command = Vector2.zero; speed = 0; }
    }
}
