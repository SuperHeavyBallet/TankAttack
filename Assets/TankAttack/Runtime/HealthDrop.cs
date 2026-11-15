using UnityEngine;
namespace TankAttack
{
    [RequireComponent(typeof(Damageable))]
    public sealed class HealthDrop : MonoBehaviour
    {
        public HealthPickup pickupPrefab;
        [Min(0)] public float healAmount = 25;
        [Min(2)] public float wreckPickupClearance = 3;
        Damageable health;
        void Awake() => health = GetComponent<Damageable>();
        void OnEnable() => health.Died += Drop;
        void OnDisable() => health.Died -= Drop;
        void Drop(Damageable unit, Team attacker)
        {
            if (pickupPrefab == null || healAmount <= 0 || unit.team != Team.Enemy || !BattleSession.IsRunning) return;
            Vector3 position = DropPosition();
            var pickup = Instantiate(pickupPrefab,position + Vector3.up * .5f,Quaternion.identity);
            pickup.amount = healAmount;
        }
        Vector3 DropPosition()
        {
            var hull=GetComponent<Collider>();
            float radius=hull != null ? new Vector2(hull.bounds.extents.x,hull.bounds.extents.z).magnitude+wreckPickupClearance : 5;
            for (int ring=0;ring<4;ring++)
                for (int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI*2/16;
                    Vector3 candidate=transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(radius+ring*3);
                    if (!CombatPhysics.Cast(candidate+Vector3.up*5,Vector3.down,15,0,transform,out var ground,includeShotVolumes:false)) continue;
                    if (ground.normal.y < .8f || ground.point.y > transform.position.y+.5f) continue;
                    candidate=ground.point;
                    // Reserve room for the player's hull to drive through the pickup.
                    if (Physics.CheckBox(candidate+Vector3.up,new Vector3(2.2f,.8f,2.2f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)) continue;
                    return candidate;
                }
            // Extremely crowded areas still drop health rather than silently losing the reward.
            return transform.position+Vector3.right*radius;
        }
    }
}
