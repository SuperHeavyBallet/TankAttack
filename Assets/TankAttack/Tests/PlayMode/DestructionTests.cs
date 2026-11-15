using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TankAttack.Tests
{
    public sealed class DestructionTests
    {
        PlayerTankInput input;
        EnemySpawner spawner;
        [UnitySetUp]
        public IEnumerator Load()
        {
            TankSelection.Clear(); LevelRun.Reset();
            yield return SceneManager.LoadSceneAsync("Assets/TankAttack/Scenes/TankAttack_Prototype.unity"); yield return null;
            input=BattleSession.Instance.player.GetComponent<PlayerTankInput>(); input.enabled=false;
            spawner=Object.FindAnyObjectByType<EnemySpawner>(); spawner.gameObject.SetActive(false);
        }
        Damageable Enemy(Vector3 position,GameObject prefab=null)
        {
            var go=Object.Instantiate(prefab != null ? prefab : spawner.enemyPrefab,position,Quaternion.identity);
            go.GetComponent<EnemyTankAI>().enabled=false; Physics.SyncTransforms(); return go.GetComponent<Damageable>();
        }
        [UnityTest]
        public IEnumerator DeathLeavesAnImmobileSolidWreckAndOnlyOneReward()
        {
            var enemy=Enemy(new Vector3(0,.68f,-12));
            var ai=enemy.GetComponent<EnemyTankAI>(); ai.enabled=true; ai.cannon.TriggerHeld=true; ai.machineGun.TriggerHeld=true;
            enemy.GetComponent<TankMotor>().Command=Vector2.up;
            int meshes=enemy.GetComponentsInChildren<MeshRenderer>().Length, deaths=0;
            enemy.Died+=(unit,team)=>deaths++;
            enemy.ApplyDamage(999,Team.Player); enemy.ApplyDamage(999,Team.Player);
            Assert.That(deaths,Is.EqualTo(1)); Assert.That(enemy.gameObject.activeInHierarchy,Is.True);
            Assert.That(enemy.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(meshes));
            Assert.That(ai.enabled,Is.False); Assert.That(enemy.GetComponent<TankMotor>().enabled,Is.False);
            Assert.That(enemy.GetComponent<TurretAim>().enabled,Is.False);
            foreach(var weapon in enemy.GetComponentsInChildren<TankWeapon>())
            { Assert.That(weapon.enabled,Is.False); Assert.That(weapon.TriggerHeld,Is.False); Assert.That(weapon.TryFire(),Is.False); }
            Assert.That(enemy.GetComponent<Rigidbody>().isKinematic,Is.True);
            Assert.That(enemy.GetComponent<BoxCollider>().enabled,Is.True);
            Assert.That(enemy.GetComponent<EnemyDestruction>().Explosion,Is.Not.Null);
            Assert.That(Object.FindObjectsByType<ExplosionLifetime>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            foreach(var source in enemy.GetComponents<AudioSource>().Where(a=>a.loop)) Assert.That(source.isPlaying,Is.False);
            Vector3 position=enemy.transform.position;
            input.motor.Command=Vector2.up; yield return new WaitForSeconds(2);
            Assert.That(enemy.transform.position,Is.EqualTo(position));
            Assert.That(input.transform.position.z,Is.LessThan(enemy.GetComponent<BoxCollider>().bounds.min.z-1.6f));
        }
        [UnityTest]
        public IEnumerator WreckCarvesNavigationAndBlocksShotsAndHealthIsAccessible()
        {
            var wreck=Enemy(new Vector3(0,.68f,-8)); wreck.ApplyDamage(999,Team.Enemy);
            yield return new WaitForSeconds(.4f);
            var obstacle=wreck.GetComponent<NavMeshObstacle>(); Assert.That(obstacle.carving,Is.True);
            Assert.That(NavMesh.SamplePosition(new Vector3(0,0,-8),out _,.2f,NavMesh.AllAreas),Is.False);
            var path=new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(new Vector3(0,0,-18),new Vector3(0,0,5),NavMesh.AllAreas,path),Is.True);
            Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(path.corners.Any(p=>Mathf.Abs(p.x)>2.8f),Is.True,"AI route must go around the wreck.");
            var target=Enemy(new Vector3(0,.68f,6));
            input.aim.enabled=false;
            Assert.That(input.cannon.CanHit(target.transform.position,target.transform),Is.False);
            input.cannon.TryFire(); input.machineGun.TryFire(); yield return new WaitForSeconds(.6f);
            Assert.That(target.Health,Is.EqualTo(target.MaxHealth));
            var pickup=Object.FindAnyObjectByType<HealthPickup>(); Assert.That(pickup,Is.Not.Null);
            Assert.That(Physics.CheckBox(pickup.transform.position+Vector3.up*.5f,new Vector3(2.2f,.8f,2.2f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore),Is.False);
        }
        [UnityTest]
        public IEnumerator ExplosionScalesWithHullAndCleansUpWhileWreckRemains()
        {
            var heavy=Enemy(new Vector3(-5,.68f,-15));
            var smallPrefab=spawner.enemyVariants.OrderBy(p=>p.transform.localScale.x).First();
            var small=Enemy(new Vector3(5,.68f,-15),smallPrefab);
            heavy.ApplyDamage(999,Team.Player); small.ApplyDamage(999,Team.Player);
            var large=heavy.GetComponent<EnemyDestruction>(); var tiny=small.GetComponent<EnemyDestruction>();
            Assert.That(large.ExplosionScale,Is.GreaterThan(tiny.ExplosionScale));
            Assert.That(large.Explosion.transform.localScale.x,Is.GreaterThan(tiny.Explosion.transform.localScale.x));
            yield return new WaitForSeconds(.12f);
            Assert.That(large.Explosion.GetComponentsInChildren<ParticleSystem>().Sum(p=>p.particleCount),Is.GreaterThan(0));
            Capture("TankAttack-explosions.png");
            yield return new WaitForSeconds(1);
            Capture("TankAttack-wreck-smoke.png");
            yield return new WaitForSeconds(3.5f);
            Assert.That(large.Explosion==null && tiny.Explosion==null,Is.True);
            Assert.That(heavy != null && small != null,Is.True);
            Assert.That(heavy.gameObject.activeInHierarchy && small.gameObject.activeInHierarchy,Is.True);
        }
        [UnityTest]
        public IEnumerator OccupiedSpawnZoneRespawnsNearbyWithoutDeletingWreck()
        {
            spawner.initialDelay=0; spawner.respawnDelay=.4f;
            spawner.gameObject.SetActive(true); spawner.StartCoroutine("Start");
            yield return new WaitForSeconds(.15f);
            int count=spawner.ActiveCount;
            var enemy=Object.FindAnyObjectByType<EnemyTankAI>().GetComponent<Damageable>(); Vector3 position=enemy.transform.position;
            int score=enemy.settings.scoreValue;
            enemy.ApplyDamage(999,Team.Player); enemy.ApplyDamage(999,Team.Player);
            Assert.That(BattleSession.Instance.Score,Is.EqualTo(score)); Assert.That(spawner.ActiveCount,Is.EqualTo(count-1));
            yield return new WaitForSeconds(.2f); Assert.That(spawner.ActiveCount,Is.EqualTo(count-1));
            yield return new WaitForSeconds(.5f); Assert.That(spawner.ActiveCount,Is.EqualTo(count));
            Assert.That(enemy != null && enemy.IsDead,Is.True); Assert.That(enemy.transform.position,Is.EqualTo(position));
            foreach(var living in Object.FindObjectsByType<Damageable>(FindObjectsSortMode.None).Where(d=>!d.IsDead && d.team==Team.Enemy))
                Assert.That(Vector3.Distance(position,living.transform.position),Is.GreaterThan(2));
        }
        static void Capture(string filename)
        {
            var camera=Camera.main; float aspect=camera.aspect; camera.aspect=16f/9;
            var texture=new RenderTexture(1280,720,24); texture.Create(); var previous=RenderTexture.active;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=texture});
            RenderTexture.active=texture; var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/"+filename),image.EncodeToPNG());
            RenderTexture.active=previous; camera.aspect=aspect; Object.Destroy(image); texture.Release(); Object.Destroy(texture);
        }
    }
}
