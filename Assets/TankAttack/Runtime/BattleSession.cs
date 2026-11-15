using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace TankAttack
{
    public sealed class BattleSession : MonoBehaviour
    {
        public static BattleSession Instance { get; private set; }
        public static bool IsRunning => Instance == null || Instance.State == RunState.Playing;
        public enum RunState { Playing, Won, Lost }
        public RunState State { get; private set; }
        public Damageable player;
        public Transform destination;
        public string menuScene = "Assets/TankAttack/Scenes/TankAttack_Menu.unity";
        public int Score { get; private set; }
        GUIStyle title, body;
        bool restarting;
        void Awake() { Instance = this; State = RunState.Playing; }
        void Start() { if (player != null) player.Died += OnPlayerDied; }
        void OnPlayerDied(Damageable unit, Team attacker) { State = RunState.Lost; Cursor.visible = true; }
        void OnDestroy() { if (Instance == this) Instance = null; Cursor.visible = true; }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { ReturnToMenu(); return; }
            // Remains available after the player object has been destroyed.
            if (State != RunState.Playing && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
        }
        public void AddScore(int points) { if (State == RunState.Playing) Score += Mathf.Max(0, points); }
        public void Complete() { if (State == RunState.Playing) { State = RunState.Won; Cursor.visible = true; } }
        public void Restart()
        { if (!restarting) { restarting = true; SceneManager.LoadScene(SceneManager.GetActiveScene().path); } }
        public void ReturnToMenu()
        {
            if(restarting || SceneUtility.GetBuildIndexByScenePath(menuScene)<0) return;
            restarting=true; CrossfadedEngineAudio.Existing?.StopEngine();
            SceneManager.LoadScene(menuScene);
        }
        void OnGUI()
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
                body = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            }
            GUI.Box(new Rect(14, 14, 380, 156), GUIContent.none);
            GUI.Label(new Rect(28, 22, 350, 35), "TANK ATTACK / PROTOTYPE", title);
            GUI.Label(new Rect(28, 60, 350, 28), $"Hull {(player != null ? player.Health : 0):0}   |   Score {Score}", body);
            GUI.Label(new Rect(28, 91, 350, 24), "WASD / arrows: drive | R: restart | Esc: menu", body);
            GUI.Label(new Rect(28, 118, 350, 24), "LMB: cannon | RMB: MG | 1-4 / wheel: swap", body);
            if (State != RunState.Playing)
            {
                GUI.Box(new Rect(Screen.width / 2 - 220, Screen.height / 2 - 70, 440, 140), GUIContent.none);
                GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2 - 50, 400, 40), State == RunState.Won ? "DESTINATION REACHED" : "TANK DESTROYED", title);
                GUI.Label(new Rect(Screen.width / 2 - 190, Screen.height / 2, 400, 30), $"Score {Score}     R: restart | Esc: tank selection", body);
                return;
            }
            if (player == null) return;
            var input = player.GetComponent<PlayerTankInput>();
            if (input == null) return;
            float distance = destination != null ? Vector3.Distance(player.transform.position, destination.position) : 0;
            GUI.Label(new Rect(28, Screen.height - 91, 1000, 26), $"Reach the blue extraction zone — {distance:0} m   |   {input.cannon.settings.DisplayName} | Ammo: {input.cannon.AmmoLabel} | Reload {input.cannon.ReadyFraction * 100:0}%", body);
            string loadout = "";
            for (int i=0;i<input.cannon.CollectedWeapons.Count;i++)
            {
                var weapon = input.cannon.CollectedWeapons[i];
                loadout += (weapon == input.cannon.settings ? "[" : "") + (i+1) + ": " + weapon.DisplayName + (weapon == input.cannon.settings ? "]" : "") + "   ";
            }
            GUI.Label(new Rect(28,Screen.height-65,1100,26),loadout,body);
            GUI.Label(new Rect(28,Screen.height-38,1000,26),"Crosshair: cannon | Center dot: machine gun | Green: clear shot | Red: turning, blocked or out of range",body);
            if (destination != null && Camera.main != null)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(destination.position + Vector3.up * 2);
                float x = Mathf.Clamp(screen.x, 80, Screen.width - 160), y = Mathf.Clamp(Screen.height - screen.y, 190, Screen.height - 110);
                GUI.Label(new Rect(x - 50, y, 160, 28), "EXTRACTION", body);
            }
            GUI.color = input.CannonOnTarget ? Color.green : Color.red;
            Vector2 p = new Vector2(input.PointerPosition.x, Screen.height - input.PointerPosition.y);
            GUI.DrawTexture(new Rect(p.x - 13, p.y - 1, 9, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(p.x + 4, p.y - 1, 9, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(p.x - 1, p.y - 13, 2, 9), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(p.x - 1, p.y + 4, 2, 9), Texture2D.whiteTexture);
            GUI.color = input.MachineGunOnTarget ? Color.green : Color.red;
            GUI.DrawTexture(new Rect(p.x-2,p.y-2,4,4),Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
