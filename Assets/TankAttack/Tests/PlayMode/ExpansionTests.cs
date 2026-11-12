using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TankAttack.Tests
{
    public sealed class ExpansionTests
    {
        BattleSession session;
        PlayerTankInput input;
        EnemySpawner spawner;
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/TankAttack/Scenes/TankAttack_Prototype.unity"); yield return null;
            session=BattleSession.Instance; input=session.player.GetComponent<PlayerTankInput>(); input.enabled=false;
            spawner=Object.FindAnyObjectByType<EnemySpawner>(); spawner.gameObject.SetActive(false);
        }
        Damageable Enemy(Vector3 position,GameObject prefab=null)
        {
            var go=Object.Instantiate(prefab!=null ? prefab : spawner.enemyPrefab,position,Quaternion.identity);
            go.GetComponent<EnemyTankAI>().enabled=false; go.GetComponent<TankMotor>().enabled=false; go.GetComponent<TurretAim>().enabled=false;
            go.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll; Physics.SyncTransforms(); return go.GetComponent<Damageable>();
        }
        WeaponSettings Weapon(int capacity=101)
        {
            var weapon=Object.Instantiate(input.cannon.settings); weapon.capacity=capacity; weapon.interval=.05f; return weapon;
        }
        [UnityTest]
        public IEnumerator ReticleStaysGreenOnEnemyVolumeAndRedBehindCover()
        {
            var enemy=Enemy(new Vector3(0,.68f,-8));
            input.ResolveAim(new Ray(enemy.transform.position+Vector3.up*20,Vector3.down));
            Assert.That(input.AimTarget,Is.EqualTo(enemy));
            yield return new WaitForSeconds(1);
            for(int i=0;i<30;i++)
            {
                enemy.transform.position+=Vector3.right*.005f; Physics.SyncTransforms();
                input.ResolveAim(new Ray(enemy.transform.position+Vector3.up*20,Vector3.down));
                yield return null; input.RefreshReticle();
                Assert.That(input.CannonOnTarget,Is.True,"A clear hit anywhere on the selected enemy should remain green.");
            }
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position=new Vector3(0,2,-15); wall.transform.localScale=new Vector3(5,4,1); Physics.SyncTransforms();
            input.RefreshReticle(); Assert.That(input.CannonOnTarget,Is.False,"Cover must turn the reticle red immediately.");
            Object.Destroy(wall); Object.Destroy(enemy.gameObject);
        }
        [UnityTest]
        public IEnumerator CannonReticleDoesNotDependOnShorterMachineGunRange()
        {
            var enemy=Enemy(new Vector3(0,.68f,30));
            input.ResolveAim(new Ray(enemy.transform.position+Vector3.up*20,Vector3.down));
            yield return new WaitForSeconds(1);
            input.RefreshReticle();
            Assert.That(input.CannonOnTarget,Is.True);
            Assert.That(input.MachineGunOnTarget,Is.False);
            Object.Destroy(enemy.gameObject);
        }
        [UnityTest]
        public IEnumerator PlacedPickupsHaveRoomToDriveAroundThem()
        {
            Physics.SyncTransforms();
            foreach(var pickup in Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))
                Assert.That(Physics.CheckBox(pickup.transform.position+Vector3.up*.4f,new Vector3(2.2f,.3f,2.2f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore),Is.False,pickup.name+" must not be buried in cover.");
            yield return null;
        }
        [UnityTest]
        public IEnumerator EnemyDropsHealthAndDrivingOverItHeals()
        {
            var enemy=Enemy(new Vector3(0,.68f,-8)); float amount=enemy.GetComponent<HealthDrop>().healAmount;
            enemy.ApplyDamage(999,Team.Player); yield return null;
            var pickup=Object.FindAnyObjectByType<HealthPickup>(); Assert.That(pickup,Is.Not.Null); Assert.That(pickup.amount,Is.EqualTo(amount));
            Assert.That(pickup.TryCollect(session.player),Is.False,"Full health should leave the pickup available.");
            session.player.ApplyDamage(40,Team.Enemy); float before=session.player.Health;
            input.GetComponent<Rigidbody>().position=pickup.transform.position+Vector3.up*.18f;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(session.player.Health,Is.EqualTo(Mathf.Min(session.player.MaxHealth,before+amount)));
            yield return null; Assert.That(pickup==null,Is.True);
        }
        [UnityTest]
        public IEnumerator HealingClampsAndCannotBeCollectedTwice()
        {
            var prefab=spawner.enemyPrefab.GetComponent<HealthDrop>().pickupPrefab;
            var pickup=Object.Instantiate(prefab,new Vector3(0,.5f,-5),Quaternion.identity); pickup.amount=500;
            session.player.ApplyDamage(5,Team.Enemy);
            Assert.That(pickup.TryCollect(session.player),Is.True); Assert.That(pickup.TryCollect(session.player),Is.False);
            Assert.That(session.player.Health,Is.EqualTo(session.player.MaxHealth)); yield return null;
        }
        [UnityTest]
        public IEnumerator PlacedWeaponPickupEquipsAndKeepsBasicWeapon()
        {
            var pickups=Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
            Assert.That(pickups.Length,Is.EqualTo(3));
            var pickup=pickups.First(p=>p.weapon.range>input.cannon.settings.range); var weapon=pickup.weapon;
            var basic=input.cannon.settings;
            input.GetComponent<Rigidbody>().position=pickup.transform.position+Vector3.up*.18f;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.That(input.cannon.settings,Is.EqualTo(weapon)); Assert.That(input.cannon.HasUnlimitedAmmo,Is.True);
            Assert.That(input.cannon.Select(0),Is.True); Assert.That(input.cannon.settings,Is.EqualTo(basic));
            input.cannon.Cycle(1); Assert.That(input.cannon.settings,Is.EqualTo(weapon));
            var blast=pickups.First(p=>p!=null && p.weapon.blastRadius>0); Assert.That(blast.TryCollect(session.player),Is.True);
            Assert.That(input.cannon.CollectedWeapons.Count,Is.EqualTo(3));
        }
        [UnityTest]
        public IEnumerator FiniteAmmoConsumesOnlyFiredShotsAndFallsBack()
        {
            var basic=input.cannon.settings; var finite=Weapon(2); finite.projectileSpeed=0; finite.damage=0;
            Assert.That(input.cannon.Collect(finite),Is.True);
            Assert.That(input.cannon.TryFire(),Is.True); Assert.That(input.cannon.RemainingAmmo,Is.EqualTo(1));
            Assert.That(input.cannon.TryFire(),Is.False); Assert.That(input.cannon.RemainingAmmo,Is.EqualTo(1));
            yield return new WaitForSeconds(.07f);
            Assert.That(input.cannon.TryFire(),Is.True); Assert.That(input.cannon.settings,Is.EqualTo(basic)); Assert.That(input.cannon.Select(1),Is.False);
            Assert.That(input.cannon.Collect(finite),Is.True); Assert.That(input.cannon.RemainingAmmo,Is.EqualTo(2));
            var zero=Weapon(0); Assert.That(input.cannon.Collect(zero),Is.False);
            input.cannon.Select(0); Object.Destroy(finite); Object.Destroy(zero);
        }
        [UnityTest]
        public IEnumerator Exactly101IsUnlimitedBut200IsFinite()
        {
            var infinite=Weapon(101); infinite.projectileSpeed=0; infinite.damage=0;
            input.cannon.Collect(infinite);
            for(int i=0;i<3;i++) { Assert.That(input.cannon.TryFire(),Is.True); yield return new WaitForSeconds(.07f); }
            Assert.That(input.cannon.RemainingAmmo,Is.EqualTo(101)); Assert.That(input.cannon.HasUnlimitedAmmo,Is.True);
            var finite=Weapon(200); finite.projectileSpeed=0; finite.damage=0; input.cannon.Collect(finite);
            Assert.That(input.cannon.TryFire(),Is.True); Assert.That(input.cannon.RemainingAmmo,Is.EqualTo(199)); Assert.That(input.cannon.HasUnlimitedAmmo,Is.False);
            input.cannon.Select(0); Object.Destroy(infinite); Object.Destroy(finite);
        }
        [UnityTest]
        public IEnumerator BlastDamagesNearbyUnitsOnceAndRespectsWallsAndRadius()
        {
            input.aim.enabled=false;
            Vector3 center=input.cannon.muzzle.position+Vector3.forward*12;
            var direct=Enemy(center); var nearby=Enemy(center+Vector3.left*3.5f); var shielded=Enemy(center+Vector3.right*4.5f); var outside=Enemy(center+Vector3.left*13);
            var extra=new GameObject("Extra hitbox"); extra.transform.SetParent(nearby.transform,false); extra.AddComponent<BoxCollider>();
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position=center+Vector3.right*2.4f; wall.transform.localScale=new Vector3(.4f,6,7); Physics.SyncTransforms();
            var blast=Weapon(); blast.damage=20; blast.blastRadius=6; blast.projectileSpeed=0;
            input.cannon.Collect(blast); Assert.That(input.cannon.TryFire(),Is.True);
            Assert.That(direct.Health,Is.EqualTo(direct.MaxHealth-20));
            Assert.That(nearby.Health,Is.EqualTo(nearby.MaxHealth-20),"Multiple colliders must not multiply blast damage.");
            Assert.That(shielded.Health,Is.EqualTo(shielded.MaxHealth)); Assert.That(outside.Health,Is.EqualTo(outside.MaxHealth));
            input.cannon.Select(0); Object.Destroy(blast); Object.Destroy(wall);
            foreach(var enemy in new[]{direct,nearby,shielded,outside}) Object.Destroy(enemy.gameObject);
            yield return null;
        }
        [UnityTest]
        public IEnumerator EnemyProjectilesAndHitscanDamageOtherEnemies()
        {
            var shooter=Enemy(new Vector3(20,.68f,-24)); var ai=shooter.GetComponent<EnemyTankAI>();
            var target=Enemy(ai.cannon.muzzle.position+Vector3.forward*12);
            float initial=target.Health;
            Assert.That(ai.cannon.TryFire(),Is.True); yield return new WaitForSeconds(.6f);
            Assert.That(target.Health,Is.EqualTo(initial-ai.cannon.settings.damage));
            Assert.That(ai.machineGun.TryFire(),Is.True);
            Assert.That(target.Health,Is.EqualTo(initial-ai.cannon.settings.damage-ai.machineGun.settings.damage));
            Assert.That(shooter.Health,Is.EqualTo(shooter.MaxHealth));
            Object.Destroy(shooter.gameObject); Object.Destroy(target.gameObject);
        }
        [UnityTest]
        public IEnumerator LightEnemiesAreFasterWeakerAndDropLessHealth()
        {
            Assert.That(spawner.enemyVariants.Length,Is.EqualTo(3));
            var heavy=spawner.enemyPrefab.GetComponent<Damageable>();
            foreach(var prefab in spawner.enemyVariants.Where(p=>p!=spawner.enemyPrefab))
            {
                var enemy=Enemy(new Vector3(0,.68f,-8),prefab);
                Assert.That(enemy.MaxHealth,Is.LessThan(heavy.settings.health));
                Assert.That(enemy.settings.forwardSpeed,Is.GreaterThan(heavy.settings.forwardSpeed));
                Assert.That(enemy.GetComponent<HealthDrop>().healAmount,Is.LessThan(heavy.GetComponent<HealthDrop>().healAmount));
                Assert.That(enemy.GetComponent<EnemyTankAI>().cannon.settings.damage,Is.LessThan(heavy.GetComponent<EnemyTankAI>().cannon.settings.damage));
                float heal=enemy.GetComponent<HealthDrop>().healAmount; enemy.ApplyDamage(999,Team.Player); yield return null;
                var pickup=Object.FindAnyObjectByType<HealthPickup>(); Assert.That(pickup.amount,Is.EqualTo(heal)); Object.Destroy(pickup.gameObject); yield return null;
            }
        }
        [UnityTest]
        public IEnumerator FriendlyKillDropsHealthRespawnsAndAwardsNoPlayerScore()
        {
            spawner.initialDelay=0; spawner.respawnDelay=.6f; spawner.gameObject.SetActive(true); spawner.StartCoroutine("Start");
            yield return new WaitForSeconds(.2f);
            int count=spawner.ActiveCount; var enemy=Object.FindAnyObjectByType<EnemyTankAI>().GetComponent<Damageable>();
            enemy.ApplyDamage(999,Team.Enemy);
            Assert.That(spawner.ActiveCount,Is.EqualTo(count-1)); Assert.That(session.Score,Is.Zero);
            yield return null; Assert.That(Object.FindAnyObjectByType<HealthPickup>(),Is.Not.Null);
            yield return new WaitForSeconds(.75f); Assert.That(spawner.ActiveCount,Is.EqualTo(count)); Assert.That(session.Score,Is.Zero);
        }
    }
}
