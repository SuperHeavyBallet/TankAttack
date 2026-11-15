using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TankAttack.Tests
{
    public sealed class MenuAudioTests
    {
        const string Menu="Assets/TankAttack/Scenes/TankAttack_Menu.unity";
        const string Game="Assets/TankAttack/Scenes/TankAttack_Prototype.unity";
        [UnitySetUp]
        public IEnumerator Prepare()
        {
            TankSelection.Clear(); yield return SceneManager.LoadSceneAsync(Menu); yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            TankSelection.Clear();
            if(CrossfadedEngineAudio.Existing!=null) Object.Destroy(CrossfadedEngineAudio.Existing.gameObject);
            yield return null;
        }
        [Test]
        public void MenuStartsTheBuildAndDisplaysCurrentStats()
        {
            Assert.That(SceneUtility.GetScenePathByBuildIndex(0),Is.EqualTo(Menu));
            var menu=Object.FindAnyObjectByType<TankMenuController>(); Assert.That(menu.SelectedIndex,Is.EqualTo(1));
            for(int i=0;i<3;i++)
            {
                menu.tankButtons[i].onClick.Invoke();
                Assert.That(menu.SelectedIndex,Is.EqualTo(i));
                Assert.That(menu.selectedName.text,Is.EqualTo(menu.roster.tanks[i].displayName));
                Assert.That(menu.statValues.text,Does.Contain(menu.roster.tanks[i].vehicle.health.ToString("0")+" HP"));
                Assert.That(TankSelection.Selected,Is.Null,"Selecting a card must not deploy before confirmation.");
                Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(Menu));
            }
        }
        IEnumerator Choose(int index)
        {
            var menu=Object.FindAnyObjectByType<TankMenuController>(); var tank=menu.roster.tanks[index];
            menu.tankButtons[index].onClick.Invoke(); menu.confirmButton.onClick.Invoke();
            Assert.That(menu.IsLoading,Is.True); Assert.That(menu.confirmButton.interactable,Is.False);
            float deadline=Time.realtimeSinceStartup+10;
            while(SceneManager.GetActiveScene().path!=Game && Time.realtimeSinceStartup<deadline) yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(Game));
            var session=BattleSession.Instance; var input=session.player.GetComponent<PlayerTankInput>(); input.enabled=false;
            Object.FindAnyObjectByType<EnemySpawner>().gameObject.SetActive(false);
            Assert.That(session.player.settings,Is.EqualTo(tank.vehicle));
            Assert.That(session.player.Health,Is.EqualTo(tank.vehicle.health));
            Assert.That(input.motor.settings,Is.EqualTo(tank.vehicle)); Assert.That(input.aim.settings,Is.EqualTo(tank.vehicle));
            Assert.That(input.cannon.settings,Is.EqualTo(tank.cannon));
            Assert.That(input.cannon.CollectedWeapons[0],Is.EqualTo(tank.cannon));
        }
        [UnityTest] public IEnumerator LightTankDeploysWithItsOwnHealthHandlingAndCannon() { yield return Choose(0); }
        [UnityTest] public IEnumerator MediumTankDeploysWithTheExistingTuning() { yield return Choose(1); }
        [UnityTest] public IEnumerator HeavyTankDeploysWithItsOwnHealthHandlingAndCannon() { yield return Choose(2); }
        [UnityTest]
        public IEnumerator EmptyUpgradeAndRestartRetainSelectedBaseTank()
        {
            yield return Choose(2);
            var tank=TankSelection.Selected; var input=BattleSession.Instance.player.GetComponent<PlayerTankInput>();
            var upgrade=Object.Instantiate(tank.cannon); upgrade.capacity=1; upgrade.projectileSpeed=0; upgrade.damage=0;
            input.cannon.Collect(upgrade); Assert.That(input.cannon.TryFire(),Is.True);
            Assert.That(input.cannon.settings,Is.EqualTo(tank.cannon));
            BattleSession.Instance.player.ApplyDamage(10,Team.Enemy);
            var previous=BattleSession.Instance; previous.Restart(); yield return null; yield return null;
            Assert.That(BattleSession.Instance,Is.Not.EqualTo(previous));
            Assert.That(BattleSession.Instance.player.Health,Is.EqualTo(tank.vehicle.health));
            Assert.That(BattleSession.Instance.player.GetComponent<PlayerTankInput>().cannon.settings,Is.EqualTo(tank.cannon));
            Assert.That(TankSelection.Selected,Is.EqualTo(tank)); Object.Destroy(upgrade);
        }
        [UnityTest]
        public IEnumerator ReturnToMenuKeepsSelectionAndReusesOneAudioService()
        {
            yield return Choose(0); var audio=CrossfadedEngineAudio.Existing;
            Assert.That(audio,Is.Not.Null); Assert.That(audio.GetComponents<AudioSource>().Length,Is.EqualTo(2));
            BattleSession.Instance.ReturnToMenu(); yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(Menu));
            Assert.That(Object.FindAnyObjectByType<TankMenuController>().SelectedIndex,Is.EqualTo(0));
            yield return Choose(1); Assert.That(CrossfadedEngineAudio.Existing,Is.SameAs(audio));
            Assert.That(Object.FindObjectsByType<CrossfadedEngineAudio>().Length,Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator EngineSchedulesOverlapsAndFadesOutOnRelease()
        {
            var settings=ScriptableObject.CreateInstance<EngineAudioSettings>(); settings.volume=.01f;
            settings.fadeInSeconds=.05f; settings.fadeOutSeconds=.1f; settings.overlapSeconds=.15f;
            settings.rumbleClip=AudioClip.Create("Quiet test rumble",26460,1,44100,false);
            var samples=new float[26460]; for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(i*2*Mathf.PI*100/44100)*.01f;
            settings.rumbleClip.SetData(samples,0);
            var owner=new GameObject("Engine test owner"); var audio=CrossfadedEngineAudio.Instance;
            audio.Bind(owner,settings); bool overlapped=false; float deadline=Time.realtimeSinceStartup+1.6f;
            while(Time.realtimeSinceStartup<deadline) { overlapped|=audio.IsOverlapping; yield return null; }
            Assert.That(audio.IsLooping,Is.True); Assert.That(overlapped,Is.True,"Successive scheduled clips must overlap.");
            Assert.That(audio.GetComponents<AudioSource>().Length,Is.EqualTo(2)); Assert.That(audio.CurrentLevel,Is.GreaterThan(.9f));
            audio.Release(owner); yield return new WaitForSecondsRealtime(.2f);
            Assert.That(audio.IsLooping,Is.False); Assert.That(audio.CurrentLevel,Is.EqualTo(0).Within(.001f));
            Object.Destroy(owner); Object.Destroy(settings.rumbleClip); Object.Destroy(settings);
        }
        [UnityTest]
        public IEnumerator MissingRumbleClipRemainsSilent()
        {
            var owner=new GameObject("Silent engine owner"); var settings=ScriptableObject.CreateInstance<EngineAudioSettings>();
            var audio=CrossfadedEngineAudio.Instance; audio.Bind(owner,settings); yield return null;
            Assert.That(audio.IsLooping,Is.False);
            foreach(var voice in audio.GetComponents<AudioSource>()) Assert.That(voice.isPlaying,Is.False);
            audio.Release(owner); Object.Destroy(owner); Object.Destroy(settings);
        }
        [UnityTest]
        public IEnumerator CaptureMenu()
        {
            yield return null; Canvas.ForceUpdateCanvases();
            var camera=Camera.main; float previousAspect=camera.aspect; camera.aspect=16f/9;
            var texture=new RenderTexture(1600,900,24); texture.Create(); var previous=RenderTexture.active;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=texture});
            RenderTexture.active=texture; var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1600,900),0,0); image.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Logs/TankAttack-menu.png"),image.EncodeToPNG());
            RenderTexture.active=previous; camera.aspect=previousAspect; Object.Destroy(image); texture.Release(); Object.Destroy(texture);
        }
    }
}
