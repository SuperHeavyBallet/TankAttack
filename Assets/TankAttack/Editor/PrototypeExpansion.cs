using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace TankAttack.Editor
{
    public static class PrototypeExpansion
    {
        const string Root = PrototypeBuilder.Root;
        const string Marker = "Pickups and Light Enemies";
        [MenuItem("Tank Attack/Apply Pickups and Light Enemies")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play mode before adding content.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (SceneManager.GetActiveScene().path != PrototypeBuilder.ScenePath) EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
            if (GameObject.Find(Marker) != null) { Debug.Log("Pickup/enemy content is already installed; keeping designer edits."); return; }
            var basic = AssetDatabase.LoadAssetAtPath<WeaponSettings>(Root + "/Settings/PlayerCannon.asset");
            if (string.IsNullOrEmpty(basic.displayName)) { basic.displayName = "Basic Cannon"; EditorUtility.SetDirty(basic); }
            var range = Clone(basic,"LongRangeCannon",w => { w.displayName="Long Range"; w.range=140; w.capacity=101; });
            var heavy = Clone(basic,"HeavyCannon",w => { w.displayName="Heavy Cannon"; w.damage=90; w.range=70; w.interval=1.8f; w.projectileSpeed=35; w.capacity=101; });
            var blast = Clone(basic,"BlastCannon",w => { w.displayName="Blast Cannon"; w.damage=35; w.range=70; w.interval=1.7f; w.projectileSpeed=40; w.blastRadius=6; w.capacity=101; });
            Material green = Mat("HealthPickup",new Color(.15f,.8f,.3f));
            Material white = Mat("PickupWhite",Color.white);
            Material cyan = Mat("RangePickup",new Color(.1f,.7f,1));
            Material orange = Mat("HeavyPickup",new Color(1,.4f,.1f));
            Material yellow = Mat("BlastPickup",new Color(1,.85f,.1f));
            var healthPrefab = HealthPrefab(green,white);
            AddHeavyHealthDrop(healthPrefab);
            var light = Enemy("LightTank",.75f,45,10,120,90,8,2,15,false,healthPrefab);
            var scout = Enemy("ScoutCar",.58f,25,13,155,125,3,.65f,8,true,healthPrefab);
            var rangePickup = WeaponPrefab("LongRangePickup",range,cyan);
            var heavyPickup = WeaponPrefab("HeavyCannonPickup",heavy,orange);
            var blastPickup = WeaponPrefab("BlastCannonPickup",blast,yellow);
            var content = new GameObject(Marker);
            Place(rangePickup,content.transform,new Vector3(-7,.5f,-17));
            Place(heavyPickup,content.transform,new Vector3(0,.5f,-11));
            Place(blastPickup,content.transform,new Vector3(7,.5f,-17));
            var spawner = UnityEngine.Object.FindAnyObjectByType<EnemySpawner>();
            if (spawner != null)
            {
                spawner.enemyVariants = new[] { spawner.enemyPrefab,light,scout };
                var points = new List<Transform>(spawner.spawnPoints);
                for (int i=0;i<2;i++)
                {
                    var point = new GameObject(i==0 ? "Light vehicle west spawn" : "Light vehicle east spawn").transform;
                    point.SetParent(content.transform); point.position=new Vector3(i==0 ? -30 : 30,.68f,-16);
                    point.rotation=Quaternion.Euler(0,i==0 ? 90 : -90,0); points.Add(point);
                }
                spawner.spawnPoints=points.ToArray(); EditorUtility.SetDirty(spawner);
            }
            EnsurePickupClearance();
            PrototypeBuilder.Rebake();
            AddInputBindings();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("TANK_ATTACK_EXPANSION_APPLIED");
        }
        public static void EnsurePickupClearance()
        {
            var root=GameObject.Find(Marker); if(root==null) return;
            Physics.SyncTransforms();
            foreach(var pickup in root.GetComponentsInChildren<WeaponPickup>())
            {
                Vector3 original=pickup.transform.position;
                bool placed=false;
                for(int ring=0;ring<=4 && !placed;ring++)
                    for(int x=-ring;x<=ring && !placed;x++)
                        for(int z=-ring;z<=ring && !placed;z++)
                        {
                            if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))!=ring) continue;
                            Vector3 candidate=original+new Vector3(x*4,0,z*4);
                            if(Physics.CheckBox(candidate+Vector3.up*.4f,new Vector3(2.2f,.3f,2.2f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)) continue;
                            // The placement must have supporting ground, rather than lie outside the arena.
                            if(!Physics.Raycast(candidate+Vector3.up,Vector3.down,out var ground,3,~0,QueryTriggerInteraction.Ignore)) continue;
                            if(ground.normal.y<.9f) continue;
                            candidate.y=ground.point.y+.5f;
                            pickup.transform.position=candidate; PrefabUtility.RecordPrefabInstancePropertyModifications(pickup.transform); placed=true;
                        }
                if(!placed) Debug.LogWarning("Could not find clear driving space for " + pickup.name,pickup);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        static T Clone<T>(T template,string name,Action<T> configure) where T : ScriptableObject
        {
            string path=Root+"/Settings/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<T>(path); if(existing!=null) return existing;
            var asset=UnityEngine.Object.Instantiate(template); asset.name=name; configure(asset);
            AssetDatabase.CreateAsset(asset,path); return asset;
        }
        static Material Mat(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path); if(material!=null) return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")); material.SetColor("_BaseColor",color);
            AssetDatabase.CreateAsset(material,path); return material;
        }
        static void Mesh(string name,Transform parent,Vector3 position,Vector3 scale,Material mat,PrimitiveType primitive=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(primitive); go.name=name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        static GameObject PickupRoot(string name, out Transform visual)
        {
            var go=new GameObject(name); var trigger=go.AddComponent<SphereCollider>(); trigger.isTrigger=true; trigger.radius=1.25f;
            visual=new GameObject("Visual").transform; visual.SetParent(go.transform,false);
            go.AddComponent<PickupPresentation>().visual=visual; return go;
        }
        static HealthPickup HealthPrefab(Material green,Material white)
        {
            string path=Root+"/Prefabs/HealthPickup.prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing.GetComponent<HealthPickup>();
            var go=PickupRoot("HealthPickup",out var visual); go.AddComponent<HealthPickup>();
            Mesh("Medical crate",visual,Vector3.zero,new Vector3(1,.35f,1),green);
            Mesh("Cross across",visual,new Vector3(0,.2f,0),new Vector3(.7f,.08f,.2f),white);
            Mesh("Cross along",visual,new Vector3(0,.2f,0),new Vector3(.2f,.08f,.7f),white);
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,path); UnityEngine.Object.DestroyImmediate(go); return prefab.GetComponent<HealthPickup>();
        }
        static GameObject WeaponPrefab(string name,WeaponSettings weapon,Material material)
        {
            string path=Root+"/Prefabs/"+name+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing;
            var go=PickupRoot(name,out var visual); go.AddComponent<WeaponPickup>().weapon=weapon;
            Mesh("Ammo crate",visual,Vector3.zero,new Vector3(1.15f,.65f,1.15f),material);
            Mesh("Shell",visual,new Vector3(0,.55f,0),new Vector3(.25f,.5f,.25f),material,PrimitiveType.Capsule);
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,path); UnityEngine.Object.DestroyImmediate(go); return prefab;
        }
        static void AddHeavyHealthDrop(HealthPickup pickup)
        {
            string path=Root+"/Prefabs/EnemyTank.prefab"; var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(go.GetComponent<HealthDrop>()!=null) return;
                var drop=go.AddComponent<HealthDrop>(); drop.pickupPrefab=pickup; drop.healAmount=25;
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        static GameObject Enemy(string name,float scale,float health,float speed,float turn,float turret,float damage,float interval,float heal,bool wheels,HealthPickup pickup)
        {
            string path=Root+"/Prefabs/"+name+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing;
            var template=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/EnemyTank.prefab");
            var vehicle=Clone(template.GetComponent<Damageable>().settings,name+"Vehicle",v=>
            { v.health=health; v.forwardSpeed=speed; v.reverseSpeed=speed*.6f; v.hullTurnSpeed=turn; v.turretTurnSpeed=turret; v.acceleration=22; v.scoreValue=wheels ? 50 : 75; });
            var templateAI=template.GetComponent<EnemyTankAI>();
            var gun=Clone(templateAI.cannon.settings,name+"Cannon",w=> { w.damage=damage; w.interval=interval; w.range=35; w.projectileSpeed=wheels ? 0 : 40; w.radius=.04f; w.capacity=101; w.blastRadius=0; });
            var mg=Clone(templateAI.machineGun.settings,name+"MachineGun",w=> { w.damage=.75f; w.interval=.3f; w.range=20; w.capacity=101; });
            var go=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(template));
            try
            {
                go.name=name; go.transform.localScale=Vector3.one*scale;
                go.GetComponent<Damageable>().settings=vehicle; go.GetComponent<TankMotor>().settings=vehicle;
                go.GetComponent<TurretAim>().settings=vehicle; go.GetComponent<Rigidbody>().mass=wheels ? 500 : 900;
                var ai=go.GetComponent<EnemyTankAI>(); ai.cannon.settings=gun; ai.machineGun.settings=mg; ai.firingDistance=wheels ? 18 : 22;
                var drop=go.GetComponent<HealthDrop>(); drop.pickupPrefab=pickup; drop.healAmount=heal;
                var visual=go.transform.Find("HullVisual (replace mesh children)");
                if(wheels && visual!=null)
                {
                    var track=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Tracks.mat");
                    var left=visual.Find("Left track"); var right=visual.Find("Right track");
                    if(left!=null) UnityEngine.Object.DestroyImmediate(left.gameObject);
                    if(right!=null) UnityEngine.Object.DestroyImmediate(right.gameObject);
                    for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
                        Mesh("Wheel",visual,new Vector3(x*1.15f,-.15f,z*1.05f),new Vector3(.6f,.85f,.85f),track,PrimitiveType.Sphere);
                }
                return PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        static void Place(GameObject prefab,Transform parent,Vector3 position)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); go.transform.SetParent(parent); go.transform.position=position;
        }
        static void AddInputBindings()
        {
            string path=Root+"/Input/TankControls.inputactions";
            var asset=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(path));
            var map=asset.FindActionMap("Tank",true);
            if(map.FindAction("CycleWeapon")==null) map.AddAction("CycleWeapon",InputActionType.Value,"<Mouse>/scroll/y");
            for(int i=1;i<=4;i++) if(map.FindAction("Weapon"+i)==null) map.AddAction("Weapon"+i,InputActionType.Button,"<Keyboard>/digit"+i);
            File.WriteAllText(path,asset.ToJson()); UnityEngine.Object.DestroyImmediate(asset); AssetDatabase.ImportAsset(path);
        }
    }
}
