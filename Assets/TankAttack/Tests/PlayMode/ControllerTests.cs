using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TankAttack.Tests
{
    public sealed class ControllerTests
    {
        PlayerTankInput input;
        EnemySpawner spawner;
        Gamepad pad;
        Mouse mouse;
        [UnitySetUp]
        public IEnumerator Load()
        {
            LevelRun.Reset(); TankSelection.Clear();
            pad=InputSystem.AddDevice<Gamepad>(); mouse=InputSystem.AddDevice<Mouse>();
            yield return SceneManager.LoadSceneAsync("Assets/TankAttack/Scenes/TankAttack_Prototype.unity");
            yield return null;
            input=BattleSession.Instance.player.GetComponent<PlayerTankInput>();
            spawner=Object.FindAnyObjectByType<EnemySpawner>(); spawner.gameObject.SetActive(false);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            yield return null;
        }
        void Send(GamepadState state)
        { InputSystem.QueueStateEvent(pad,state); InputSystem.Update(); input.ReadControls(); }
        [Test]
        public void AnalogDriveKeepsTankAxesAndRejectsStickDrift()
        {
            Send(new GamepadState {leftStick=new Vector2(.05f,.05f),rightStick=new Vector2(.05f,.05f)});
            Assert.That(input.UsingGamepad,Is.False); Assert.That(input.motor.Command,Is.EqualTo(Vector2.zero));
            Send(new GamepadState {leftStick=Vector2.up});
            Assert.That(input.UsingGamepad,Is.True); Assert.That(input.motor.Command.y,Is.EqualTo(1).Within(.01f));
            Send(new GamepadState {leftStick=Vector2.down});
            Assert.That(input.motor.Command.y,Is.EqualTo(-1).Within(.01f));
            Send(new GamepadState {leftStick=Vector2.right});
            Assert.That(input.motor.Command.x,Is.EqualTo(1).Within(.01f)); Assert.That(input.motor.Command.y,Is.Zero);
            Send(new GamepadState {leftStick=new Vector2(0,.6f)});
            Assert.That(input.motor.Command.y,Is.InRange(.1f,.9f));
            Send(new GamepadState()); Assert.That(input.motor.Command,Is.EqualTo(Vector2.zero));
        }
        [UnityTest]
        public IEnumerator StickAimsGraduallyHoldsDirectionAndMouseTakesOver()
        {
            Quaternion before=input.aim.yawPivot.rotation;
            Send(new GamepadState {rightStick=Vector2.right});
            Assert.That(input.UsingGamepad,Is.True); Assert.That(Vector3.Dot(input.StickAimDirection,Vector3.right),Is.GreaterThan(.99f));
            Assert.That(Quaternion.Angle(before,input.aim.yawPivot.rotation),Is.LessThan(1));
            Send(new GamepadState());
            var direction=input.StickAimDirection;
            yield return new WaitForSeconds(1.2f);
            input.ReadControls();
            Assert.That(input.StickAimDirection,Is.EqualTo(direction));
            Assert.That(Vector3.Dot(input.aim.yawPivot.forward,Vector3.right),Is.GreaterThan(.99f));
            Assert.That(Mathf.Abs(input.cannon.muzzle.forward.y),Is.LessThan(.001f));
            Vector2 cursor=Camera.main.WorldToScreenPoint(input.transform.position+Vector3.forward*15);
            InputSystem.QueueStateEvent(mouse,new MouseState {position=cursor}); InputSystem.Update(); input.ReadControls();
            Assert.That(input.UsingGamepad,Is.False); Assert.That(input.PointerPosition,Is.EqualTo(cursor)); Assert.That(input.HasAimPoint,Is.True);
            Assert.That(input.aim.Target.z,Is.GreaterThan(input.transform.position.z+5));
            Send(new GamepadState {rightStick=Vector2.left}); Assert.That(input.UsingGamepad,Is.True);
            Assert.That(Vector3.Dot(input.StickAimDirection,Vector3.left),Is.GreaterThan(.99f));
        }
        [Test]
        public void TriggersAndShouldersFireAndCycleOncePerPress()
        {
            var upgrade=Object.Instantiate(input.cannon.settings); upgrade.capacity=5;
            input.cannon.Collect(upgrade); input.cannon.Select(0);
            Send(new GamepadState {rightStick=Vector2.up,rightTrigger=1,leftTrigger=1});
            Assert.That(input.cannon.TriggerHeld,Is.True); Assert.That(input.machineGun.TriggerHeld,Is.True);
            Assert.That(input.cannon.TryFire(),Is.True); Assert.That(input.cannon.TryFire(),Is.False);
            Send(new GamepadState().WithButton(GamepadButton.RightShoulder));
            Assert.That(input.cannon.settings,Is.SameAs(upgrade)); Assert.That(input.cannon.TryFire(),Is.False,"Switching must retain the reload timer.");
            Send(new GamepadState().WithButton(GamepadButton.RightShoulder)); Assert.That(input.cannon.settings,Is.SameAs(upgrade));
            Send(new GamepadState());
            Assert.That(input.cannon.TriggerHeld,Is.False); Assert.That(input.machineGun.TriggerHeld,Is.False);
            Send(new GamepadState().WithButton(GamepadButton.LeftShoulder)); Assert.That(input.cannon.settings,Is.Not.SameAs(upgrade));
            Object.Destroy(upgrade);
        }
        [Test]
        public void DisconnectStopsDrivingAndFiring()
        {
            Send(new GamepadState {leftStick=Vector2.up,rightTrigger=1});
            Assert.That(input.motor.Command.y,Is.GreaterThan(.9f)); Assert.That(input.cannon.TriggerHeld,Is.True);
            InputSystem.RemoveDevice(pad);
            Assert.That(input.UsingGamepad,Is.False); Assert.That(input.motor.Command,Is.EqualTo(Vector2.zero));
            Assert.That(input.cannon.TriggerHeld,Is.False); Assert.That(input.machineGun.TriggerHeld,Is.False);
        }
        [Test]
        public void DualShockUsesTheSameGenericBindings()
        {
            InputSystem.RemoveDevice(pad); pad=InputSystem.AddDevice<DualShockGamepad>();
            Send(new GamepadState {leftStick=Vector2.up,rightStick=Vector2.left,rightTrigger=1,leftTrigger=1});
            Assert.That(input.UsingGamepad,Is.True); Assert.That(input.motor.Command.y,Is.GreaterThan(.9f));
            Assert.That(input.StickAimDirection.x,Is.LessThan(-.99f));
            Assert.That(input.cannon.TriggerHeld && input.machineGun.TriggerHeld,Is.True);
        }
        [UnityTest]
        public IEnumerator DirectionalReticleUsesRealWeaponContactAndRange()
        {
            var enemy=Object.Instantiate(spawner.enemyPrefab,new Vector3(0,.68f,30),Quaternion.identity);
            enemy.GetComponent<EnemyTankAI>().enabled=false; enemy.GetComponent<TankMotor>().enabled=false;
            enemy.GetComponent<Rigidbody>().constraints=RigidbodyConstraints.FreezeAll;
            Physics.SyncTransforms();
            Send(new GamepadState {rightStick=Vector2.up});
            yield return new WaitForSeconds(.2f); input.ReadControls(); input.RefreshReticle();
            Assert.That(input.AimTarget,Is.EqualTo(enemy.GetComponent<Damageable>()));
            Assert.That(input.CannonOnTarget,Is.True); Assert.That(input.MachineGunOnTarget,Is.False);
            // A blocker in the muzzle path must invalidate the formerly selected target immediately.
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position=input.cannon.muzzle.position+Vector3.forward*5;
            wall.transform.localScale=new Vector3(4,5,1); Physics.SyncTransforms();
            input.RefreshReticle(); Assert.That(input.CannonOnTarget,Is.False);
            Object.Destroy(wall); Object.Destroy(enemy);
        }
        [UnityTest]
        public IEnumerator MenuCanBeNavigatedAndConfirmedUsingGamepad()
        {
            yield return SceneManager.LoadSceneAsync("Assets/TankAttack/Scenes/TankAttack_Menu.unity"); yield return null;
            var menu=Object.FindAnyObjectByType<TankMenuController>();
            var events=EventSystem.current; events.SendMessage("OnApplicationFocus",true);
            var module=(InputSystemUIInputModule)events.currentInputModule;
            Assert.That(events.currentSelectedGameObject,Is.Not.Null,"Menu must establish controller focus.");
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadRight)); InputSystem.Update();
            Assert.That(pad.dpad.ReadValue().x,Is.EqualTo(1),"Virtual pad state must reach the input system.");
            Assert.That(module.move.action.enabled,Is.True,"UI navigation must be enabled.");
            Assert.That(module.move.action.ReadValue<Vector2>().x,Is.EqualTo(1),"UI action must receive the D-pad binding.");
            events.currentInputModule.Process();
            Assert.That(menu.SelectedIndex,Is.EqualTo(2));
            InputSystem.QueueStateEvent(pad,new GamepadState()); InputSystem.Update(); events.currentInputModule.Process();
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadDown)); InputSystem.Update(); events.currentInputModule.Process();
            Assert.That(events.currentSelectedGameObject,Is.EqualTo(menu.confirmButton.gameObject));
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update(); events.currentInputModule.Process();
            Assert.That(menu.IsLoading,Is.True);
            string gameplay=menu.roster.gameplayScene;
            float deadline=Time.realtimeSinceStartup+20;
            while (SceneManager.GetActiveScene().path != gameplay && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Does.EndWith("TankAttack_Generated.unity"));
            InputSystem.QueueStateEvent(pad,new GamepadState()); InputSystem.Update();
        }
        [UnityTest]
        public IEnumerator ControllerCanRestartAfterDeathAndReturnToMenu()
        {
            var old=BattleSession.Instance;
            old.player.ApplyDamage(999,Team.Enemy);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.Select)); InputSystem.Update();
            old.SendMessage("Update"); yield return null; yield return null;
            Assert.That(BattleSession.Instance,Is.Not.SameAs(old)); Assert.That(BattleSession.IsRunning,Is.True);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.Start)); InputSystem.Update();
            BattleSession.Instance.SendMessage("Update"); yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Does.EndWith("TankAttack_Menu.unity"));
        }
    }
}
