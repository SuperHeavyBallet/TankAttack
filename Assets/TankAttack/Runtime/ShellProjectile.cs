using UnityEngine;
namespace TankAttack
{
    public sealed class ShellProjectile : MonoBehaviour
    {
        WeaponSettings weapon;
        Team team;
        Transform owner;
        Vector3 direction;
        float remaining;
        public void Launch(WeaponSettings settings, Team shooter, Transform shooterRoot, Vector3 heading)
        { weapon = settings; team = shooter; owner = shooterRoot; direction = heading.normalized; remaining = settings.range; }
        void FixedUpdate()
        {
            if (!BattleSession.IsRunning) { Destroy(gameObject); return; }
            float distance = Mathf.Min(remaining, weapon.projectileSpeed * Time.fixedDeltaTime);
            Vector3 start = transform.position;
            bool blocked = CombatPhysics.Cast(start, direction, distance, weapon.radius, owner, out var hit);
            Vector3 end = blocked ? hit.point : start + direction * distance;
            ShotVisual.Trace(start, end, weapon, weapon.radius * 2, .1f);
            transform.position = end; remaining -= distance;
            if (blocked) CombatPhysics.Hit(hit, weapon, team, owner);
            if (blocked || remaining <= 0) Destroy(gameObject);
        }
    }
}
