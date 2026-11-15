using System.Collections;
using UnityEngine;
using UnityEngine.AI;
namespace TankAttack
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        public GameObject enemyPrefab;
        [Tooltip("Optional per-slot pattern. Empty uses enemyPrefab for every slot.")]
        public GameObject[] enemyVariants;
        public bool randomizeVariants;
        public int variantSeed;
        System.Random variantRandom;
        public Transform[] spawnPoints;
        [Min(0)] public float respawnDelay = 8;
        [Min(0)] public float initialDelay = 2;
        [Min(0)] public float minimumPlayerDistance = 12;
        [Tooltip("Search nearby navigable ground when the original zone is occupied by a wreck or another vehicle.")]
        [Min(0)] public float blockedSpawnSearchRadius = 12;
        public int ActiveCount { get; private set; }
        IEnumerator Start()
        {
            variantRandom = new System.Random(variantSeed);
            yield return new WaitForSeconds(initialDelay);
            for (int i=0;i<spawnPoints.Length;i++) StartCoroutine(SpawnSlot(spawnPoints[i],0,PrefabForSlot(i)));
        }
        GameObject PrefabForSlot(int index)
        {
            if (enemyVariants == null || enemyVariants.Length == 0) return enemyPrefab;
            int choice = randomizeVariants ? variantRandom.Next(enemyVariants.Length) : index % enemyVariants.Length;
            return enemyVariants[choice] != null ? enemyVariants[choice] : enemyPrefab;
        }
        IEnumerator SpawnSlot(Transform point, float delay, GameObject prefab)
        {
            yield return new WaitForSeconds(delay);
            while (BattleSession.IsRunning)
            {
                if (TryFindSpawnPosition(point.position,out var position))
                {
                    var unit = Instantiate(prefab, position, point.rotation).GetComponent<Damageable>();
                    ActiveCount++;
                    unit.Died += (dead, attacker) =>
                    {
                        ActiveCount--;
                        if (attacker == Team.Player) BattleSession.Instance?.AddScore(dead.settings.scoreValue);
                        if (isActiveAndEnabled) StartCoroutine(SpawnSlot(point, respawnDelay, randomizeVariants ? PrefabForSlot(0) : prefab));
                    };
                    yield break;
                }
                yield return new WaitForSeconds(1);
            }
        }
        bool CanSpawn(Vector3 position)
        {
            var player = BattleSession.Instance != null ? BattleSession.Instance.player : null;
            if (player != null && Vector3.Distance(player.transform.position, position) < minimumPlayerDistance) return false;
            foreach (var collider in Physics.OverlapBox(position + Vector3.up * .4f, new Vector3(1.7f, .6f, 2.1f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (collider != null) return false;
            return true;
        }
        bool TryFindSpawnPosition(Vector3 origin,out Vector3 position)
        {
            position=origin;
            if (CanSpawn(origin)) return true;
            for (float radius=4;radius<=blockedSpawnSearchRadius;radius+=4)
                for (int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI*2/16;
                    Vector3 candidate=origin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
                    if (!NavMesh.SamplePosition(candidate,out var nav,1.5f,NavMesh.AllAreas)) continue;
                    candidate=nav.position+Vector3.up*.68f;
                    if (!CanSpawn(candidate)) continue;
                    position=candidate; return true;
                }
            return false;
        }
    }
}
