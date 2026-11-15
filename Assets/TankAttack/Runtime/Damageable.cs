using System;
using UnityEngine;
namespace TankAttack
{
    public enum Team { Player, Enemy }
    public sealed class Damageable : MonoBehaviour
    {
        public VehicleSettings settings;
        public Team team;
        public float Health { get; private set; }
        public float MaxHealth => settings != null ? settings.health : 100;
        public float Heal(float amount)
        {
            if (IsDead || amount <= 0) return 0;
            float old = Health; Health = Mathf.Min(MaxHealth,Health + amount); return Health - old;
        }
        public bool IsDead { get; private set; }
        public void RestoreHealth(float health)
        { if (!IsDead) Health = Mathf.Clamp(health,1,MaxHealth); }
        public event Action<Damageable, Team> Died;
        [Min(0), Tooltip("Minimum world-space target height for horizontal shots. Does not affect movement collisions. Zero disables the extra hit volume.")]
        public float shotTargetHeight = 3;
        public Collider ShotCollider { get; private set; }
        void Awake()
        {
            Health = settings != null ? settings.health : 100;
            var hull = GetComponent<BoxCollider>();
            if (hull == null || shotTargetHeight <= 0) return;
            var volume = new GameObject("Shot hit volume");
            volume.transform.SetParent(transform, false);
            var box = volume.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Vector3 size = hull.size;
            float bottom = hull.center.y - size.y * .5f;
            size.y = Mathf.Max(size.y, shotTargetHeight / Mathf.Max(.001f, Mathf.Abs(transform.lossyScale.y)));
            box.size = size;
            box.center = new Vector3(hull.center.x, bottom + size.y * .5f, hull.center.z);
            ShotCollider = box;
        }
        public void ApplyDamage(float amount, Team attacker)
        {
            if (IsDead || (attacker == Team.Player && team == Team.Player) || amount <= 0) return;
            Health = Mathf.Max(0, Health - amount);
            if (Health > 0) return;
            IsDead = true;
            GetComponent<VehicleAudio>()?.DestroySound();
            Died?.Invoke(this, attacker);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
