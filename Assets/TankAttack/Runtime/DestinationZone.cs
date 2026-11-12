using UnityEngine;
namespace TankAttack
{
    public sealed class DestinationZone : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var health = other.GetComponentInParent<Damageable>();
            if (health != null && health.team == Team.Player && !health.IsDead) BattleSession.Instance?.Complete();
        }
    }
}
