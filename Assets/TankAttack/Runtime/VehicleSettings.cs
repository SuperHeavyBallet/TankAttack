using UnityEngine;
namespace TankAttack
{
    [CreateAssetMenu(menuName = "Tank Attack/Vehicle Settings")]
    public sealed class VehicleSettings : ScriptableObject
    {
        [Min(1)] public float health = 100;
        [Min(0)] public float forwardSpeed = 9;
        [Min(0)] public float reverseSpeed = 5;
        [Min(.1f)] public float acceleration = 14;
        [Min(.1f)] public float braking = 22;
        [Min(1)] public float hullTurnSpeed = 85;
        [Min(1)] public float turretTurnSpeed = 65;
        [Min(1)] public float elevationSpeed = 45;
        [Min(0)] public int scoreValue = 100;
    }
}
