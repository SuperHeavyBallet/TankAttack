using UnityEngine;
namespace TankAttack
{
    [CreateAssetMenu(menuName = "Tank Attack/Weapon Settings")]
    public sealed class WeaponSettings : ScriptableObject
    {
        public const int UnlimitedCapacity = 101;
        public string displayName;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        [Tooltip("Exactly 101 means unlimited. Every other nonnegative value is starting ammo, spent once per fired shot.")]
        [Min(0)] public int capacity = UnlimitedCapacity;
        [Tooltip("Zero: direct damage only. Positive: full damage to other units in this radius with a clear blast path; no double damage on the direct target.")]
        [Min(0)] public float blastRadius;
        [Min(0)] public float damage = 40;
        [Min(.01f)] public float interval = 1.2f;
        [Min(1)] public float range = 80;
        [Tooltip("Zero uses hitscan. Positive values launch a swept projectile.")]
        [Min(0)] public float projectileSpeed = 45;
        [Min(.01f)] public float radius = .08f;
        [Min(.01f)] public float aimTolerance = .6f;
        public Material tracerMaterial;
        public Color tracerColor = new Color(1, .75f, .2f);
        public AudioClip fireClip;
        public AudioClip impactClip;
        [Range(0,1)] public float volume = .7f;
    }
}
