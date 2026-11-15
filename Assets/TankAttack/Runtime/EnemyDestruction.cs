using UnityEngine;
using UnityEngine.AI;
namespace TankAttack
{
    [RequireComponent(typeof(Damageable))]
    public sealed class EnemyDestruction : MonoBehaviour
    {
        public ParticleSystem explosionPrefab;
        public ParticleSystem wreckSmokePrefab;
        [Min(.1f)] public float explosionSizeMultiplier = 1;
        [Tooltip("Hull length corresponding to the explosion prefab's authored size.")]
        [Min(.1f)] public float referenceHullSize = 3.6f;
        [Min(0)] public float explosionHeightOffset = .3f;
        [Min(0)] public float navigationClearance = 2.3f;
        public bool IsWreck { get; private set; }
        public ParticleSystem Explosion { get; private set; }
        public ParticleSystem WreckSmoke { get; private set; }
        public float ExplosionScale { get; private set; }
        public void BecomeWreck()
        {
            if (IsWreck || !GetComponent<Damageable>().IsDead) return;
            IsWreck=true;
            var ai=GetComponent<EnemyTankAI>(); if (ai != null) ai.enabled=false;
            var motor=GetComponent<TankMotor>(); if (motor != null) { motor.Command=Vector2.zero; motor.enabled=false; }
            var aim=GetComponent<TurretAim>(); if (aim != null) aim.enabled=false;
            foreach (var weapon in GetComponentsInChildren<TankWeapon>()) { weapon.TriggerHeld=false; weapon.enabled=false; }
            GetComponent<VehicleAudio>()?.StopLoops();
            var body=GetComponent<Rigidbody>();
            if (body != null)
            {
                if (!body.isKinematic) { body.linearVelocity=Vector3.zero; body.angularVelocity=Vector3.zero; }
                body.isKinematic=true; body.constraints=RigidbodyConstraints.FreezeAll;
            }
            var hull=GetComponent<BoxCollider>();
            Vector3 scale=transform.lossyScale;
            scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
            Vector3 size=hull != null ? Vector3.Scale(hull.size,scale) : scale;
            ExplosionScale=Mathf.Max(.1f,Mathf.Max(size.x,size.z)/Mathf.Max(.1f,referenceHullSize)*explosionSizeMultiplier);
            if (hull != null)
            {
                var obstacle=GetComponent<NavMeshObstacle>();
                if (obstacle == null) obstacle=gameObject.AddComponent<NavMeshObstacle>();
                // Runtime maps bake a larger agent radius than the project's default agent.
                // Carving uses that registered agent radius, so explicitly supply the difference.
                float radius=BattleSession.Instance?.generatedLevel != null ? BattleSession.Instance.generatedLevel.settings.navigationAgentRadius : navigationClearance;
                float padding=Mathf.Max(0,Mathf.Max(navigationClearance,radius)-NavMesh.GetSettingsByID(0).agentRadius);
                obstacle.shape=NavMeshObstacleShape.Box; obstacle.center=hull.center;
                obstacle.size=hull.size+new Vector3(padding*2/Mathf.Max(.01f,scale.x),0,padding*2/Mathf.Max(.01f,scale.z));
                obstacle.carveOnlyStationary=true; obstacle.carvingTimeToStationary=0; obstacle.carving=true;
            }
            if (explosionPrefab != null)
            {
                Vector3 origin=hull != null ? new Vector3(hull.bounds.center.x,hull.bounds.max.y,hull.bounds.center.z) : transform.position;
                Explosion=Instantiate(explosionPrefab,origin+Vector3.up*explosionHeightOffset,Quaternion.identity);
                Explosion.transform.localScale*=ExplosionScale;
                Explosion.Play(true);

                WreckSmoke = Instantiate(wreckSmokePrefab, origin, Quaternion.identity);
                WreckSmoke.transform.localScale*=ExplosionScale;
                WreckSmoke.Play(true);
            }
            // Retain the original render hierarchy, physical hull and shot volume as cover.
            gameObject.name += " (Wreck)";
        }
    }
}
