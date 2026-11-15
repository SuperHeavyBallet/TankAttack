using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TankAttack.Tests
{
    public sealed class GeneratedLevelTests
    {
        const string Scene="Assets/TankAttack/Scenes/TankAttack_Generated.unity";
        GeneratedLevel level;
        [UnitySetUp]
        public IEnumerator Load()
        {
            LevelRun.Reset(); TankSelection.Clear();
            yield return SceneManager.LoadSceneAsync(Scene);
            yield return WaitReady();
        }
        IEnumerator WaitReady()
        {
            float deadline=Time.realtimeSinceStartup+30;
            do { yield return null; level=Object.FindAnyObjectByType<GeneratedLevel>(); }
            while ((level == null || !level.Ready) && Time.realtimeSinceStartup<deadline);
            Assert.That(level,Is.Not.Null); Assert.That(level.Error,Is.Null); Assert.That(level.Ready,Is.True);
            level.session.player.GetComponent<PlayerTankInput>().enabled=false;
            level.spawner.gameObject.SetActive(false);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        { LevelRun.Reset(); yield return null; }
        [Test]
        public void DimensionsBandsClearancesAndRoutes()
        {
            Assert.That(level.Length,Is.InRange(50,75)); Assert.That(level.Width,Is.InRange(50,level.Length));
            int bands=level.settings.BandCount(level.Length);
            Assert.That(level.spawner.spawnPoints.Length,Is.EqualTo(bands*2));
            Assert.That(level.PickupPositions.Count,Is.EqualTo(bands*3));
            Assert.That(level.session.destination.position.z,Is.EqualTo(level.Length-7).Within(.01f));
            Assert.That(level.Reachable(level.session.destination.position),Is.True);
            for(int i=0;i<level.spawner.spawnPoints.Length;i++)
            {
                var p=level.spawner.spawnPoints[i].position;
                Assert.That(p.z,Is.InRange((i/2)*50,Mathf.Min(level.Length,(i/2+1)*50)));
                Assert.That(Mathf.Sign(p.x),Is.EqualTo(i%2==0 ? -1 : 1));
                Assert.That(level.Reachable(p),Is.True);
            }
            foreach(var p in level.PickupPositions) Assert.That(level.Reachable(p),Is.True);
            Assert.That(level.TerrainBounds.Count,Is.GreaterThan(0));
            for(int i=0;i<level.TerrainBounds.Count;i++)
            {
                var a=level.TerrainBounds[i];
                Assert.That(a.min.x,Is.GreaterThanOrEqualTo(-level.Width*.5f+level.Clearance));
                Assert.That(a.max.x,Is.LessThanOrEqualTo(level.Width*.5f-level.Clearance));
                for(int j=i+1;j<level.TerrainBounds.Count;j++)
                {
                    var b=level.TerrainBounds[j];
                    float gapX=Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x);
                    float gapZ=Mathf.Max(a.min.z-b.max.z,b.min.z-a.max.z);
                    Assert.That(Mathf.Max(gapX,gapZ),Is.GreaterThanOrEqualTo(level.Clearance-.01f));
                }
            }
            foreach(var pickup in Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))
                Assert.That(Physics.CheckBox(pickup.transform.position+Vector3.up*.4f,new Vector3(2.2f,.3f,2.2f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore),Is.False);
        }
        [Test]
        public void GrowthAndMaximumPopulationAreBounded()
        {
            Assert.That(level.settings.BandCount(50),Is.EqualTo(1));
            Assert.That(level.settings.BandCount(300),Is.EqualTo(6));
            for(int seed=0;seed<100;seed++)
            {
                var early=level.settings.Dimensions(1,new System.Random(seed));
                var late=level.settings.Dimensions(100,new System.Random(seed));
                Assert.That(early.y,Is.InRange(50,75));
                Assert.That(late.y,Is.EqualTo(300)); Assert.That(late.x,Is.InRange(50,200));
                Assert.That(late.x,Is.LessThanOrEqualTo(late.y));
            }
        }
        [UnityTest]
        public IEnumerator SmallWideAndMaximumMapsHaveCompleteNavigation()
        {
            foreach (var size in new[] { new Vector2(50,50),new Vector2(200,300),new Vector2(75,151) })
            {
                var configuration=Object.Instantiate(level.settings);
                configuration.minimumLength=configuration.maximumLength=configuration.firstLevelMaximumLength=size.y;
                configuration.minimumWidth=configuration.maximumWidth=size.x;
                configuration.fixedSeed=123+(int)size.y;
                LevelRun.Reset();
                UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,LoadSceneMode> configure=(scene,mode)=>Object.FindAnyObjectByType<GeneratedLevel>().settings=configuration;
                SceneManager.sceneLoaded+=configure;
                try { yield return SceneManager.LoadSceneAsync(Scene); }
                finally { SceneManager.sceneLoaded-=configure; }
                yield return WaitReady();
                Assert.That(level.Length,Is.EqualTo(size.y)); Assert.That(level.Width,Is.EqualTo(size.x));
                Assert.That(level.spawner.spawnPoints.Length,Is.EqualTo(Mathf.FloorToInt(size.y/50)*2));
                Assert.That(level.PickupPositions.Count,Is.EqualTo(Mathf.FloorToInt(size.y/50)*3));
                Assert.That(level.TerrainBounds.Count,Is.GreaterThan(0));
                Assert.That(level.Reachable(level.session.destination.position),Is.True);
                foreach(var point in level.spawner.spawnPoints) Assert.That(level.Reachable(point.position),Is.True);
                foreach(var point in level.PickupPositions) Assert.That(level.Reachable(point),Is.True);
                if (size.y == 300) CaptureOverview();
                // Keep the live generator's settings valid until its scene unloads.
                Object.Destroy(configuration,2);
            }
        }
        void CaptureOverview()
        {
            var camera=Camera.main;
            camera.GetComponent<FollowCamera>().enabled=false;
            camera.transform.SetPositionAndRotation(new Vector3(0,250,150),Quaternion.Euler(90,0,0));
            camera.orthographicSize=160; camera.aspect=.75f; camera.farClipPlane=500;
            var texture=new RenderTexture(768,1024,24); texture.Create(); var previous=RenderTexture.active;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=texture});
            RenderTexture.active=texture; var image=new Texture2D(768,1024,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,768,1024),0,0); image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/TankAttack-generated.png"),image.EncodeToPNG());
            RenderTexture.active=previous; Object.Destroy(image); texture.Release(); Object.Destroy(texture);
        }
        [UnityTest]
        public IEnumerator ExtractionAdvancesOnceAndPreservesRunThenRestartReusesMap()
        {
            var old=level.session; var input=old.player.GetComponent<PlayerTankInput>();
            var weapon=Object.Instantiate(input.cannon.settings); weapon.capacity=5; weapon.damage=0; weapon.projectileSpeed=0;
            input.cannon.Collect(weapon); input.cannon.TryFire();
            float health=old.player.Health-10; old.player.ApplyDamage(10,Team.Enemy); old.AddScore(75);
            old.player.GetComponent<Rigidbody>().position=old.destination.position+Vector3.up;
            float deadline=Time.realtimeSinceStartup+30;
            while(BattleSession.Instance==old && Time.realtimeSinceStartup<deadline) yield return null;
            yield return WaitReady();
            Assert.That(LevelRun.Level,Is.EqualTo(2)); Assert.That(level.LevelNumber,Is.EqualTo(2));
            Assert.That(level.session.Score,Is.EqualTo(75)); Assert.That(level.session.player.Health,Is.EqualTo(health));
            input=level.session.player.GetComponent<PlayerTankInput>();
            Assert.That(input.cannon.settings,Is.EqualTo(weapon)); Assert.That(input.cannon.RemainingAmmo,Is.EqualTo(4));
            float length=level.Length, width=level.Width; int seed=level.Seed;
            Vector3 spawn=level.spawner.spawnPoints[0].position;
            level.session.AddScore(99); level.session.Restart(); yield return null; yield return WaitReady();
            Assert.That(LevelRun.Level,Is.EqualTo(2)); Assert.That(level.Seed,Is.EqualTo(seed));
            Assert.That(level.Length,Is.EqualTo(length)); Assert.That(level.Width,Is.EqualTo(width));
            Assert.That(level.spawner.spawnPoints[0].position,Is.EqualTo(spawn)); Assert.That(level.session.Score,Is.EqualTo(75));
            Object.Destroy(weapon);
        }
        [UnityTest]
        public IEnumerator EachZoneSpawnsOneEnemyAndRespawnsAfterDelay()
        {
            level.spawner.initialDelay=0; level.spawner.respawnDelay=.3f;
            level.spawner.gameObject.SetActive(true); level.spawner.StartCoroutine("Start");
            yield return new WaitForSeconds(.15f);
            Assert.That(level.spawner.ActiveCount,Is.EqualTo(level.spawner.spawnPoints.Length));
            var enemy=Object.FindAnyObjectByType<EnemyTankAI>().GetComponent<Damageable>();
            enemy.ApplyDamage(999,Team.Enemy); Assert.That(level.session.Score,Is.Zero);
            Assert.That(level.spawner.ActiveCount,Is.EqualTo(level.spawner.spawnPoints.Length-1));
            yield return new WaitForSeconds(.5f);
            Assert.That(level.spawner.ActiveCount,Is.EqualTo(level.spawner.spawnPoints.Length));
        }
    }
}
