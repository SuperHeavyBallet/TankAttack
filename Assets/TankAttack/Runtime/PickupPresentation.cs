using UnityEngine;
namespace TankAttack
{
    public sealed class PickupPresentation : MonoBehaviour
    {
        public Transform visual;
        public string label;
        public Color labelColor = Color.white;
        public float spinSpeed = 35;
        Vector3 origin;
        GUIStyle style;
        void Start() { if (visual != null) origin = visual.localPosition; }
        void Update()
        {
            if (visual == null) return;
            visual.Rotate(0,spinSpeed * Time.deltaTime,0,Space.Self);
            visual.localPosition = origin + Vector3.up * Mathf.Sin(Time.time * 2) * .12f;
        }
        void OnGUI()
        {
            var camera = Camera.main;
            if (camera == null || !BattleSession.IsRunning) return;
            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
            if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };
            var rect = new Rect(screen.x-90,Screen.height-screen.y-14,180,28);
            GUI.color = new Color(0,0,0,.7f); GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color = labelColor; GUI.Label(rect,label,style); GUI.color = Color.white;
        }
    }
}
