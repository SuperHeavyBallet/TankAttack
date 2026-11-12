using UnityEngine;
namespace TankAttack
{
    public sealed class WeaponPickup : MonoBehaviour
    {
        public WeaponSettings weapon;
        bool consumed;
        void Start()
        {
            var presentation = GetComponent<PickupPresentation>();
            if (presentation != null && weapon != null) presentation.label = weapon.DisplayName;
        }
        void OnTriggerEnter(Collider other) => TryCollect(other.GetComponentInParent<Damageable>());
        public bool TryCollect(Damageable collector)
        {
            if (consumed || !BattleSession.IsRunning || collector == null || collector.IsDead || collector.team != Team.Player) return false;
            var input = collector.GetComponent<PlayerTankInput>();
            if (input == null || !input.cannon.Collect(weapon)) return false;
            consumed = true; gameObject.SetActive(false); Destroy(gameObject); return true;
        }
    }
}
