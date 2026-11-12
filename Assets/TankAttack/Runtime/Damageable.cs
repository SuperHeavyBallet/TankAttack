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
        public event Action<Damageable, Team> Died;
        void Awake() => Health = settings != null ? settings.health : 100;
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
