using UnityEngine;
namespace TankAttack
{
    [RequireComponent(typeof(Damageable))]
    public sealed class HealthDrop : MonoBehaviour
    {
        public HealthPickup pickupPrefab;
        [Min(0)] public float healAmount = 25;
        Damageable health;
        void Awake() => health = GetComponent<Damageable>();
        void OnEnable() => health.Died += Drop;
        void OnDisable() => health.Died -= Drop;
        void Drop(Damageable unit, Team attacker)
        {
            if (pickupPrefab == null || healAmount <= 0 || unit.team != Team.Enemy || !BattleSession.IsRunning) return;
            Vector3 position = transform.position;
            if (CombatPhysics.Cast(position+Vector3.up*3,Vector3.down,10,0,transform,out var ground)) position = ground.point;
            var pickup = Instantiate(pickupPrefab,position + Vector3.up * .5f,Quaternion.identity);
            pickup.amount = healAmount;
        }
    }
}
