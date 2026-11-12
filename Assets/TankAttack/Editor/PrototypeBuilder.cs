using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
namespace TankAttack.Editor
{
    public static class PrototypeBuilder
    {
        public const string Root = "Assets/TankAttack";
        public const string ScenePath = Root + "/Scenes/TankAttack_Prototype.unity";
        [MenuItem("Tank Attack/Create or Open Prototype")]
        public static void Create()
        {
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string folder in new[] { "Scenes", "Prefabs", "Settings", "Materials", "Input" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var grey = Material("Ground", new Color(.29f,.31f,.33f));
            var wall = Material("Obstacles", new Color(.48f,.49f,.5f));
            var playerMat = Material("Player", new Color(.42f,.52f,.43f));
            var enemyMat = Material("Enemy", new Color(.6f,.4f,.38f));
            var dark = Material("Tracks", new Color(.16f,.17f,.18f));
            var blue = Material("Extraction", new Color(.15f,.58f,.75f));
            var playerSettings = Asset<VehicleSettings>("PlayerVehicle");
            var enemySettings = Asset<VehicleSettings>("EnemyTankVehicle");
            enemySettings.forwardSpeed = 5; enemySettings.reverseSpeed = 3; enemySettings.hullTurnSpeed = 65;
            enemySettings.turretTurnSpeed = 38; enemySettings.health = 80;
            var cannon = Asset<WeaponSettings>("PlayerCannon");
            var mg = Asset<WeaponSettings>("PlayerMachineGun");
            mg.damage = 4; mg.interval = .09f; mg.projectileSpeed = 0; mg.radius = .015f; mg.range = 45;
            var enemyCannon = Asset<WeaponSettings>("EnemyCannon");
            enemyCannon.damage = 14; enemyCannon.interval = 2.8f; enemyCannon.projectileSpeed = 30;
            var enemyMg = Asset<WeaponSettings>("EnemyMachineGun");
            enemyMg.damage = 1; enemyMg.interval = .2f; enemyMg.projectileSpeed = 0; enemyMg.radius = .015f; enemyMg.range = 30;
            foreach (var asset in new UnityEngine.Object[] { playerSettings, enemySettings, cannon, mg, enemyCannon, enemyMg }) EditorUtility.SetDirty(asset);
            EnsureTracerMaterials();
            var input = Input();
            var playerPrefab = Tank("PlayerTank", Team.Player, playerSettings, cannon, mg, playerMat, dark, input);
            var enemyPrefab = Tank("EnemyTank", Team.Enemy, enemySettings, enemyCannon, enemyMg, enemyMat, dark, input);
            var environment = new GameObject("Environment (rebake navigation after editing)");
            Cube("Ground", environment.transform, new Vector3(0,-.5f,20), new Vector3(80,1,110), grey, true);
            Cube("West wall", environment.transform, new Vector3(-40,2,20), new Vector3(2,4,110), wall, true);
            Cube("East wall", environment.transform, new Vector3(40,2,20), new Vector3(2,4,110), wall, true);
            Cube("South wall", environment.transform, new Vector3(0,2,-35), new Vector3(80,4,2), wall, true);
            Cube("North wall", environment.transform, new Vector3(0,2,75), new Vector3(80,4,2), wall, true);
            Cube("Cover A", environment.transform, new Vector3(-10,2,-7), new Vector3(13,4,7), wall, true);
            Cube("Cover B", environment.transform, new Vector3(12,2,10), new Vector3(15,4,8), wall, true);
            Cube("Cover C", environment.transform, new Vector3(-15,2,26), new Vector3(10,4,14), wall, true);
            Cube("Cover D", environment.transform, new Vector3(9,2,45), new Vector3(18,4,7), wall, true);
            Cube("Cover E", environment.transform, new Vector3(-18,2,58), new Vector3(8,4,6), wall, true);
            var surface = environment.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Bake(surface);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0,.68f,-24);
            var destination = Cube("Extraction Zone", null, new Vector3(0,.035f,65), new Vector3(11,.06f,8), blue, false);
            var trigger = destination.AddComponent<BoxCollider>(); trigger.isTrigger = true;
            // Keep marker scale separate from the trigger's world height.
            trigger.size = new Vector3(1,50,1); trigger.center = new Vector3(0,25,0);
            destination.AddComponent<DestinationZone>();
            var session = new GameObject("Battle Session").AddComponent<BattleSession>();
            session.player = player.GetComponent<Damageable>(); session.destination = destination.transform;
            var camera = new GameObject("Main Camera"); camera.tag = "MainCamera";
            var cam = camera.AddComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 24; cam.farClipPlane = 220;
            cam.backgroundColor = new Color(.15f,.17f,.2f); cam.clearFlags = CameraClearFlags.SolidColor;
            camera.AddComponent<AudioListener>();
            var follow = camera.AddComponent<FollowCamera>(); follow.target = player.transform;
            camera.transform.position = player.transform.position + follow.offset; camera.transform.LookAt(player.transform);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(50,-35,0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.6f,.64f,.7f);
            var spawner = new GameObject("Enemy Director").AddComponent<EnemySpawner>(); spawner.enemyPrefab = enemyPrefab;
            spawner.spawnPoints = new Transform[4];
            Vector3[] positions = { new Vector3(-28,.68f,2), new Vector3(28,.68f,28), new Vector3(-28,.68f,46), new Vector3(27,.68f,64) };
            for (int i=0; i<positions.Length; i++)
            {
                var point = new GameObject("Spawn " + (i+1)).transform; point.SetParent(spawner.transform);
                point.position = positions[i]; point.rotation = Quaternion.Euler(0,180,0); spawner.spawnPoints[i] = point;
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var existing in EditorBuildSettings.scenes) if (existing.path != ScenePath) scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("TANK_ATTACK_PROTOTYPE_CREATED " + ScenePath);
        }
        [MenuItem("Tank Attack/Rebake Prototype Navigation")]
        public static void Rebake()
        {
            var surface = UnityEngine.Object.FindAnyObjectByType<NavMeshSurface>();
            if (surface == null) throw new InvalidOperationException("Open the prototype scene first.");
            Bake(surface); EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
        }
        static void Bake(NavMeshSurface surface)
        {
            Physics.SyncTransforms();
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(surface.transform, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            var settings = NavMesh.GetSettingsByID(surface.agentTypeID);
            settings.agentRadius = 2.3f; settings.agentHeight = 2; settings.agentClimb = .3f; settings.agentSlope = 35;
            settings.overrideVoxelSize = true; settings.voxelSize = .2f;
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(new Vector3(0,2,20), new Vector3(90,12,120)), Vector3.zero, Quaternion.identity);
            if (data == null) throw new InvalidOperationException("Navigation bake failed.");
            surface.RemoveData();
            string assetPath = Root + "/Scenes/PrototypeNavigation.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);
            if (existing == null) { AssetDatabase.CreateAsset(data, assetPath); existing = data; }
            else { EditorUtility.CopySerialized(data, existing); UnityEngine.Object.DestroyImmediate(data); EditorUtility.SetDirty(existing); }
            surface.navMeshData = existing; surface.AddData(); EditorUtility.SetDirty(surface);
        }
        public static void EnsureTracerMaterials()
        {
            string path = Root + "/Materials/Tracer.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetColor("_BaseColor", new Color(1,.75f,.2f));
                AssetDatabase.CreateAsset(mat,path);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:WeaponSettings", new[] { Root + "/Settings" }))
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (weapon.tracerMaterial != null) continue;
                weapon.tracerMaterial = mat; EditorUtility.SetDirty(weapon);
            }
            AssetDatabase.SaveAssets();
        }
        static T Asset<T>(string name) where T : ScriptableObject
        {
            string path = Root + "/Settings/" + name + ".asset";
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value,path); }
            return value;
        }
        static Material Material(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(mat,path); }
            return mat;
        }
        static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, bool collision)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static Transform Pivot(string name, Transform parent, Vector3 position)
        { var pivot = new GameObject(name).transform; pivot.SetParent(parent,false); pivot.localPosition = position; return pivot; }
        static GameObject Tank(string name, Team team, VehicleSettings vehicle, WeaponSettings cannon, WeaponSettings mg, Material hull, Material track, InputActionAsset input)
        {
            var root = new GameObject(name);
            var health = root.AddComponent<Damageable>(); health.settings = vehicle; health.team = team;
            var body = root.AddComponent<Rigidbody>(); body.mass = 1800; body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            var box = root.AddComponent<BoxCollider>(); box.size = new Vector3(2.8f,1.3f,3.6f);
            root.AddComponent<VehicleAudio>();
            var motor = root.AddComponent<TankMotor>(); motor.settings = vehicle;
            var visual = Pivot("HullVisual (replace mesh children)",root.transform,Vector3.zero);
            Cube("Hull", visual, Vector3.zero, new Vector3(2.2f,.9f,3.3f),hull,false);
            Cube("Left track", visual,new Vector3(-1.2f,-.1f,0),new Vector3(.45f,.9f,3.5f),track,false);
            Cube("Right track", visual,new Vector3(1.2f,-.1f,0),new Vector3(.45f,.9f,3.5f),track,false);
            var yaw = Pivot("TurretYaw",root.transform,new Vector3(0,.65f,0));
            Cube("TurretVisual",yaw,Vector3.zero,new Vector3(1.65f,.65f,1.7f),hull,false);
            var pitch = Pivot("GunPitch",yaw,new Vector3(0,.05f,0));
            Cube("CannonVisual",pitch,new Vector3(0,0,1.5f),new Vector3(.22f,.22f,2.4f),track,false);
            Cube("CoaxVisual",pitch,new Vector3(.36f,0,1),new Vector3(.09f,.09f,1.5f),track,false);
            var aim = root.AddComponent<TurretAim>(); aim.settings = vehicle; aim.yawPivot = yaw; aim.pitchPivot = pitch;
            var main = pitch.gameObject.AddComponent<TankWeapon>(); main.settings = cannon;
            main.muzzle = Pivot("CannonMuzzle",pitch,new Vector3(0,0,2.75f)); main.breech = pitch;
            var secondary = yaw.gameObject.AddComponent<TankWeapon>(); secondary.settings = mg;
            secondary.muzzle = Pivot("MachineGunMuzzle",pitch,new Vector3(.36f,0,1.8f)); secondary.breech = pitch;
            if (team == Team.Player)
            {
                var control = root.AddComponent<PlayerTankInput>(); control.actions = input; control.motor = motor; control.aim = aim;
                control.cannon = main; control.machineGun = secondary;
            }
            else
            {
                var control = root.AddComponent<EnemyTankAI>(); control.motor = motor; control.aim = aim;
                control.cannon = main; control.machineGun = secondary;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root,Root + "/Prefabs/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root); return prefab;
        }
        static InputActionAsset Input()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap("Tank");
            var move = map.AddAction("Move",InputActionType.Value, expectedControlLayout:"Vector2");
            move.AddCompositeBinding("2DVector(mode=1)").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            move.AddCompositeBinding("2DVector(mode=1)").With("Up","<Keyboard>/upArrow").With("Down","<Keyboard>/downArrow").With("Left","<Keyboard>/leftArrow").With("Right","<Keyboard>/rightArrow");
            map.AddAction("Point",InputActionType.Value,"<Mouse>/position",expectedControlLayout:"Vector2");
            map.AddAction("Cannon",InputActionType.Button,"<Mouse>/leftButton");
            map.AddAction("MachineGun",InputActionType.Button,"<Mouse>/rightButton");
            map.AddAction("Restart",InputActionType.Button,"<Keyboard>/r");
            string path = Root + "/Input/TankControls.inputactions";
            File.WriteAllText(path,asset.ToJson()); UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
    }
}
