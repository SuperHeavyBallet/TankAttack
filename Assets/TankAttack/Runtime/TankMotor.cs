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
        void Awake() => body = GetComponent<Rigidbody>();
        void FixedUpdate()
        {
            if (settings == null) return;
            var input = BattleSession.IsRunning ? new Vector2(Mathf.Clamp(Command.x, -1, 1), Mathf.Clamp(Command.y, -1, 1)) : Vector2.zero;
            float desired = input.y * (input.y >= 0 ? settings.forwardSpeed : settings.reverseSpeed);
            float rate = Mathf.Abs(desired) < Mathf.Abs(speed) || desired * speed < 0 ? settings.braking : settings.acceleration;
            speed = Mathf.MoveTowards(speed, desired, rate * Time.fixedDeltaTime);
            body.angularVelocity = new Vector3(0, input.x * settings.hullTurnSpeed * Mathf.Deg2Rad, 0);
            Vector3 velocity = transform.forward * speed;
            velocity.y = body.linearVelocity.y;
            body.linearVelocity = velocity;
        }
        void OnDisable() { Command = Vector2.zero; speed = 0; }
    }
}
