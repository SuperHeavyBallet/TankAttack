using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TankAttack.Tests
{
    public sealed class PrototypeTests
    {
        BattleSession session;
        PlayerTankInput input;
        EnemySpawner spawner;
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/TankAttack/Scenes/TankAttack_Prototype.unity");
            yield return null;
            session = BattleSession.Instance;
            Assert.That(session, Is.Not.Null);
            input = session.player.GetComponent<PlayerTankInput>(); input.enabled = false;
            spawner = Object.FindAnyObjectByType<EnemySpawner>(); spawner.gameObject.SetActive(false);
        }
        [UnityTest]
        public IEnumerator DriveTurnReverseAndSolidCover()
        {
            var motor = input.motor;
            Vector3 start = motor.transform.position;
            motor.Command = new Vector2(0,1);
            yield return new WaitForSeconds(.6f);
            Assert.That(motor.transform.position.z, Is.GreaterThan(start.z + 1));
            motor.Command = new Vector2(0,-1);
            yield return new WaitForSeconds(.8f);
            Assert.That(Vector3.Dot(motor.GetComponent<Rigidbody>().linearVelocity,motor.transform.forward), Is.LessThan(-1));
            motor.Command = new Vector2(1,0);
            yield return new WaitForSeconds(.5f);
            Assert.That(Quaternion.Angle(motor.transform.rotation,Quaternion.identity), Is.GreaterThan(20));
            var body = motor.GetComponent<Rigidbody>(); body.position = new Vector3(-10,.68f,-17); body.rotation = Quaternion.identity;
            motor.Command = new Vector2(0,1);
            yield return new WaitForSeconds(1.5f);
            Assert.That(body.position.z, Is.LessThan(-12f), "Hull must stop before cover A.");
        }
        [UnityTest]
        public IEnumerator TurretTakesTimeAndOcclusionTurnsReticleRed()
        {
            var aim = input.aim;
            aim.Target = aim.pitchPivot.position + Vector3.right * 20;
            Quaternion before = aim.yawPivot.rotation;
            yield return null;
            Assert.That(Quaternion.Angle(before,aim.yawPivot.rotation), Is.LessThan(15));
            Assert.That(input.cannon.CanHit(aim.Target), Is.False);
            yield return new WaitForSeconds(1.8f);
            Assert.That(input.cannon.CanHit(aim.Target), Is.True);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = aim.pitchPivot.position + Vector3.right * 10;
            wall.transform.localScale = new Vector3(1,5,6);
            Physics.SyncTransforms();
            Assert.That(input.cannon.CanHit(aim.Target), Is.False);
            Object.Destroy(wall);
        }
        [UnityTest]
        public IEnumerator HullStaysUprightWhileTurningAgainstCover()
        {
            var body = input.motor.GetComponent<Rigidbody>();
            body.position = new Vector3(-10,.68f,-17);
            body.rotation = Quaternion.identity;
            var rammer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rammer.transform.position = body.position + new Vector3(6,.7f,0);
            rammer.transform.localScale = new Vector3(2,2,3);
            var otherBody = rammer.AddComponent<Rigidbody>();
            otherBody.mass = 2500;
            otherBody.useGravity = false;
            otherBody.linearVelocity = Vector3.left * 12;
            Physics.SyncTransforms();
            input.motor.Command = new Vector2(.25f,1);
            for (int i=0;i<150;i++)
            {
                // Exercise real contact impulses while steering, rather than scripted torque.
                yield return new WaitForFixedUpdate();
                Assert.That(Vector3.Angle(body.rotation * Vector3.up, Vector3.up), Is.LessThan(.1f));
            }
            input.motor.Command = Vector2.zero;
            Assert.That(otherBody.linearVelocity.x, Is.GreaterThan(-10), "The rammer must have made physical contact.");
            Object.Destroy(rammer);
        }
        [UnityTest]
        public IEnumerator GunsStayHorizontalForNearAndElevatedCursor()
        {
            foreach (var offset in new[] { new Vector3(.1f,-10,.1f), new Vector3(10,30,10), Vector3.zero })
            {
                input.aim.Target = input.aim.yawPivot.position + offset;
                for (int i=0;i<10;i++)
                {
                    yield return null;
                    Assert.That(Mathf.Abs(input.cannon.muzzle.forward.y), Is.LessThan(.0001f));
                    Assert.That(Mathf.Abs(input.machineGun.muzzle.forward.y), Is.LessThan(.0001f));
                }
            }
            input.aim.Target = input.aim.yawPivot.position + Vector3.forward * 15;
            yield return new WaitForSeconds(2);
            var ground = input.aim.Target; ground.y = 0;
            Assert.That(input.cannon.CanHit(ground), Is.True, "Ground cursor uses horizontal range/alignment.");
        }
        [UnityTest]
        public IEnumerator HorizontalWeaponsHitSmallScoutAndRespectCover()
        {
            GameObject smallest = spawner.enemyPrefab;
            foreach (var variant in spawner.enemyVariants)
                if (variant != null && variant.transform.localScale.y < smallest.transform.localScale.y) smallest = variant;
            var enemy = Object.Instantiate(smallest, new Vector3(0,.4f,-8), Quaternion.identity);
            enemy.GetComponent<EnemyTankAI>().enabled = false;
            enemy.GetComponent<TankMotor>().enabled = false;
            enemy.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            var health = enemy.GetComponent<Damageable>();
            input.aim.Target = enemy.transform.position;
            yield return new WaitForSeconds(1);
            Physics.SyncTransforms();
            Assert.That(input.cannon.CanHit(enemy.transform.position, enemy.transform), Is.True);
            Assert.That(input.machineGun.CanHit(enemy.transform.position, enemy.transform), Is.True);
            float before = health.Health;
            input.machineGun.TryFire();
            Assert.That(health.Health, Is.EqualTo(before - input.machineGun.settings.damage));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = (input.transform.position + enemy.transform.position) * .5f + Vector3.up;
            wall.transform.localScale = new Vector3(5,5,1);
            Physics.SyncTransforms();
            Assert.That(input.cannon.CanHit(enemy.transform.position, enemy.transform), Is.False);
            Assert.That(input.machineGun.CanHit(enemy.transform.position, enemy.transform), Is.False);
            Object.Destroy(wall);
            Object.Destroy(enemy);
        }
        GameObject EnemyInFront()
        {
            var enemy = Object.Instantiate(spawner.enemyPrefab, input.cannon.muzzle.position + Vector3.forward * 12, Quaternion.identity);
            enemy.GetComponent<EnemyTankAI>().enabled = false;
            enemy.GetComponent<TankMotor>().enabled = false;
            enemy.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            Physics.SyncTransforms(); return enemy;
        }
        [UnityTest]
        public IEnumerator CannonTravelsAndMachineGunDealsDamage()
        {
            input.aim.enabled = false;
            var enemy = EnemyInFront(); var health = enemy.GetComponent<Damageable>();
            float initial = health.Health;
            Assert.That(input.cannon.TryFire(), Is.True);
            Assert.That(health.Health, Is.EqualTo(initial), "Cannon damage must wait for shell arrival.");
            Assert.That(input.cannon.TryFire(), Is.False, "Reload must prevent a second shot.");
            yield return new WaitForSeconds(.5f);
            Assert.That(health.Health, Is.EqualTo(initial - input.cannon.settings.damage));
            Assert.That(input.machineGun.TryFire(), Is.True);
            Assert.That(health.Health, Is.EqualTo(initial - input.cannon.settings.damage - input.machineGun.settings.damage));
            Object.Destroy(enemy);
        }
        [UnityTest]
        public IEnumerator CoverBlocksBothWeapons()
        {
            input.aim.enabled = false;
            var enemy = EnemyInFront(); var health = enemy.GetComponent<Damageable>(); float initial = health.Health;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = input.cannon.muzzle.position + Vector3.forward * 5;
            wall.transform.localScale = new Vector3(5,5,1); Physics.SyncTransforms();
            input.cannon.TryFire(); input.machineGun.TryFire();
            yield return new WaitForSeconds(.6f);
            Assert.That(health.Health, Is.EqualTo(initial));
            Object.Destroy(enemy); Object.Destroy(wall);
        }
        [UnityTest]
        public IEnumerator DelayedRespawnAwardsScoreOnce()
        {
            spawner.initialDelay = 0; spawner.respawnDelay = .6f; spawner.gameObject.SetActive(true);
            // Start ran before deactivation, so explicitly restart its lifecycle coroutine.
            spawner.StartCoroutine("Start");
            yield return new WaitForSeconds(.2f);
            Assert.That(spawner.ActiveCount, Is.EqualTo(spawner.spawnPoints.Length));
            var enemy = Object.FindAnyObjectByType<EnemyTankAI>().GetComponent<Damageable>();
            enemy.ApplyDamage(999,Team.Player); enemy.ApplyDamage(999,Team.Player);
            Assert.That(session.Score, Is.EqualTo(enemy.settings.scoreValue));
            Assert.That(spawner.ActiveCount, Is.EqualTo(spawner.spawnPoints.Length - 1));
            yield return new WaitForSeconds(.25f);
            Assert.That(spawner.ActiveCount, Is.EqualTo(spawner.spawnPoints.Length - 1));
            yield return new WaitForSeconds(.5f);
            Assert.That(spawner.ActiveCount, Is.EqualTo(spawner.spawnPoints.Length));
        }
        [UnityTest]
        public IEnumerator NavigationReachesAroundObstacles()
        {
            var path = new NavMeshPath();
            Assert.That(NavMesh.SamplePosition(new Vector3(-28,0,2), out var start, 3, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.SamplePosition(input.transform.position, out var end, 3, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            yield return null;
        }
        [UnityTest]
        public IEnumerator DestinationCompletesRunAndStopsWeapons()
        {
            input.GetComponent<Rigidbody>().position = session.destination.position + Vector3.up;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(session.State, Is.EqualTo(BattleSession.RunState.Won));
            Assert.That(input.cannon.TryFire(), Is.False);
        }
        [UnityTest]
        public IEnumerator EnemyAcquiresPlayerAndFires()
        {
            var enemy = Object.Instantiate(spawner.enemyPrefab, new Vector3(0,.68f,-12), Quaternion.Euler(0,180,0));
            float health = session.player.Health;
            yield return new WaitForSeconds(2);
            Assert.That(session.player.Health, Is.LessThan(health), "Enemy must fire at the player's collider, not reject its interior aim point as an obstruction.");
            Object.Destroy(enemy);
        }
        [UnityTest]
        public IEnumerator CaptureRenderedArena()
        {
            yield return null;
            var camera = Camera.main;
            var texture = new RenderTexture(1280,720,24);
            texture.Create();
            var previous = RenderTexture.active;
            float aspect = camera.aspect;
            camera.aspect = 1280f / 720;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = texture });
            RenderTexture.active = texture;
            var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/TankAttack-preview.png"),image.EncodeToPNG());
            RenderTexture.active = previous; camera.aspect = aspect;
            Object.Destroy(image); texture.Release(); Object.Destroy(texture);
        }
        [UnityTest]
        public IEnumerator DeathEndsRunAndFriendlyDamageIsIgnored()
        {
            float initial = session.player.Health;
            session.player.ApplyDamage(50,Team.Player);
            Assert.That(session.player.Health, Is.EqualTo(initial));
            session.player.ApplyDamage(999,Team.Enemy);
            Assert.That(session.State, Is.EqualTo(BattleSession.RunState.Lost));
            yield return null;
        }
    }
}
