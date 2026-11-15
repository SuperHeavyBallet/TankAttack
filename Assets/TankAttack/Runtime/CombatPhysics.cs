using System.Collections.Generic;
using UnityEngine;
namespace TankAttack
{
    public static class CombatPhysics
    {
        // Only designated shot triggers count; pickups and other trigger volumes never block shots.
        public static bool Cast(Vector3 origin, Vector3 direction, float distance, float radius, Transform owner, out RaycastHit nearest, bool includeShotVolumes = true)
        {
            nearest = default;
            if (distance <= 0 || direction.sqrMagnitude < .0001f) return false;
            var hits = radius > 0
                ? Physics.SphereCastAll(origin, radius, direction.normalized, distance, ~0, QueryTriggerInteraction.Collide)
                : Physics.RaycastAll(origin, direction.normalized, distance, ~0, QueryTriggerInteraction.Collide);
            float best = float.PositiveInfinity;
            foreach (var hit in hits)
            {
                if (owner != null && hit.transform.IsChildOf(owner)) continue;
                if (hit.collider.isTrigger && (!includeShotVolumes || !IsShotVolume(hit.collider))) continue;
                if (hit.distance >= best) continue;
                best = hit.distance; nearest = hit;
            }
            return best < float.PositiveInfinity;
        }
        static bool IsShotVolume(Collider collider) => collider.GetComponentInParent<Damageable>()?.ShotCollider == collider;
        public static void Hit(RaycastHit hit, WeaponSettings weapon, Team team, Transform owner = null)
        {
            var direct = hit.collider.GetComponentInParent<Damageable>();
            direct?.ApplyDamage(weapon.damage, team);
            VehicleAudio.At(weapon.impactClip, hit.point, weapon.volume);
            if (weapon.blastRadius <= 0) return;
            Vector3 center = hit.point + hit.normal * .05f;
            var damaged = new HashSet<Damageable>();
            if (direct != null) damaged.Add(direct);
            foreach (var collider in Physics.OverlapSphere(center,weapon.blastRadius,~0,QueryTriggerInteraction.Collide))
            {
                if (collider.isTrigger && !IsShotVolume(collider)) continue;
                var target = collider.GetComponentInParent<Damageable>();
                if (target == null || target.IsDead || damaged.Contains(target) || (owner != null && target.transform.IsChildOf(owner))) continue;
                Vector3 closest = collider.ClosestPoint(center);
                Vector3 delta = closest - center;
                if (delta.sqrMagnitude > .0001f && Cast(center,delta,delta.magnitude + .02f,0,owner,out var blocker)
                    && blocker.collider.GetComponentInParent<Damageable>() != target) continue;
                damaged.Add(target);
                target.ApplyDamage(weapon.damage,team);
            }
            BlastVisual.Show(center,weapon);
        }
    }
}
