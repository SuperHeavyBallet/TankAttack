using UnityEngine;
namespace TankAttack
{
    public sealed class HealthPickup : MonoBehaviour
    {
        [Min(0)] public float amount = 20;
        [Tooltip("Seconds before an uncollected drop disappears. Zero keeps it indefinitely.")]
        [Min(0)] public float lifetime = 90;
        bool consumed;
        void Start()
        {
            if (lifetime > 0) Destroy(gameObject,lifetime);
            var presentation = GetComponent<PickupPresentation>();
            if (presentation != null) presentation.label = "+" + amount.ToString("0") + " health";
        }
        void OnTriggerEnter(Collider other) => TryCollect(other.GetComponentInParent<Damageable>());
        void OnTriggerStay(Collider other) => TryCollect(other.GetComponentInParent<Damageable>());
        public bool TryCollect(Damageable collector)
        {
            if (consumed || !BattleSession.IsRunning || collector == null || collector.team != Team.Player || collector.IsDead || collector.Heal(amount) <= 0) return false;
            consumed = true; gameObject.SetActive(false); Destroy(gameObject); return true;
        }
    }
}
