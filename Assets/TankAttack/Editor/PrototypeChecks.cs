using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace TankAttack.Editor
{
    [InitializeOnLoad]
    public static class PrototypeChecks
    {
        const string Request = "Temp/TankAttack.RunChecks.request";
        static double nextCheck;
        static PrototypeChecks()
        {
            TestRunnerApi.RegisterTestCallback(ScriptableObject.CreateInstance<ResultWriter>());
            EditorApplication.update += CheckRequest;
        }
        static void CheckRequest()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            string request = File.ReadAllText(Request);
            File.Delete(Request);
            if (request.StartsWith("Refresh")) { AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); return; }
            if (request.StartsWith("MenuSetupSaved"))
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                Debug.Log("MENU_SETUP_ACTIVE_SCENE: " + scene.path + " dirty=" + scene.isDirty);
                if (scene.path == PrototypeBuilder.ScenePath || scene.path == TankMenuBuilder.MenuPath)
                    EditorSceneManager.SaveScene(scene);
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            { Debug.LogWarning("Save your scene, then use Tank Attack > Run Gameplay Checks."); return; }
            if (request.StartsWith("MenuOpen")) { TankMenuBuilder.OpenMenu(); return; }
            if (request.StartsWith("GenerationSetup")) GeneratedLevelBuilder.Apply();
            if (request.StartsWith("MenuSetup")) TankMenuBuilder.Apply();
            if (request.StartsWith("Expand")) PrototypeExpansion.Apply();
            if (request.StartsWith("Clearance"))
            {
                PrototypeExpansion.EnsurePickupClearance();
                PrototypeBuilder.Rebake();
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
            Run();
        }
        [MenuItem("Tank Attack/Run Gameplay Checks")]
        public static void Run()
        {
            PrototypeBuilder.Create();
            PrototypeBuilder.EnsureTracerMaterials();
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode, assemblyNames = new[] { "TankAttack.PlayModeTests" } }));
        }
        public sealed class ResultWriter : ScriptableObject, ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Logs");
                TestRunnerApi.SaveResultToFile(result,"Logs/TankAttack-PlayMode.xml");
                Debug.Log($"TANK_ATTACK_CHECKS: {result.PassCount} passed, {result.FailCount} failed.");
            }
        }
    }
}
