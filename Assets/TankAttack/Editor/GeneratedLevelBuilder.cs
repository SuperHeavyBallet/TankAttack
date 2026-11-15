using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TankAttack.Editor
{
    public static class GeneratedLevelBuilder
    {
        public const string ScenePath = "Assets/TankAttack/Scenes/TankAttack_Generated.unity";
        const string SettingsPath = "Assets/TankAttack/Settings/LevelGeneration.asset";
        [MenuItem("Tank Attack/Set Up Generated Levels")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var settings = AssetDatabase.LoadAssetAtPath<LevelGenerationSettings>(SettingsPath);
            if (!File.Exists(ScenePath))
            {
                var original = EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
                // Save a separate copy before changing any scene objects.
                EditorSceneManager.SaveScene(original,ScenePath,true);
                var scene = EditorSceneManager.OpenScene(ScenePath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<LevelGenerationSettings>();
                    settings.groundMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/TankAttack/Materials/Ground.mat");
                    settings.wallMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/TankAttack/Materials/Obstacles.mat");
                    settings.weaponPickups = new[] { "LongRangePickup", "HeavyCannonPickup", "BlastCannonPickup" }.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TankAttack/Prefabs/"+name+".prefab")).ToArray();
                    var blocks = new List<GameObject>();
                    foreach (var source in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c => c.name.StartsWith("Cover ")).OrderBy(c => c.name))
                    {
                        string path="Assets/TankAttack/Prefabs/Terrain"+source.name.Replace(" ","")+".prefab";
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null)
                        {
                            var root=new GameObject("Terrain "+source.name);
                            var visual=Object.Instantiate(source.gameObject,root.transform);
                            visual.name=source.name; visual.transform.position=new Vector3(0,source.transform.position.y*.6f,0); visual.transform.localScale=source.transform.localScale*.6f;
                            prefab=PrefabUtility.SaveAsPrefabAsset(root,path); Object.DestroyImmediate(root);
                        }
                        blocks.Add(prefab);
                    }
                    settings.terrainPrefabs=blocks.ToArray();
                    AssetDatabase.CreateAsset(settings,SettingsPath);
                }
                foreach (var surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None)) Object.DestroyImmediate(surface.gameObject);
                foreach (var pickup in Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None)) Object.DestroyImmediate(pickup.gameObject);
                var session=Object.FindAnyObjectByType<BattleSession>();
                var spawner=Object.FindAnyObjectByType<EnemySpawner>();
                foreach (var point in spawner.spawnPoints) if (point != null) Object.DestroyImmediate(point.gameObject);
                spawner.spawnPoints=new Transform[0];
                var generator=new GameObject("Generated Level").AddComponent<GeneratedLevel>();
                generator.settings=settings; generator.session=session; generator.spawner=spawner;
                session.generatedLevel=generator;
                EditorSceneManager.SaveScene(scene);
            }
            else EditorSceneManager.OpenScene(ScenePath);
            var roster=AssetDatabase.LoadAssetAtPath<TankRoster>("Assets/TankAttack/Settings/PlayerTankRoster.asset");
            if (roster != null && roster.gameplayScene == PrototypeBuilder.ScenePath)
            { roster.gameplayScene=ScenePath; EditorUtility.SetDirty(roster); }
            var scenes=EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Insert(Mathf.Min(1,scenes.Count),new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("TANK_ATTACK_GENERATED_LEVELS_READY "+ScenePath);
        }
    }
}
