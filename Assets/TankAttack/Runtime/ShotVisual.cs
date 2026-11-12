using UnityEngine;
namespace TankAttack
{
    public sealed class ShotVisual : MonoBehaviour
    {
        static Material material;
        public static void Trace(Vector3 start, Vector3 end, WeaponSettings settings, float width, float lifetime = .07f)
        {
            if (settings.tracerMaterial == null && material == null) material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            var go = new GameObject("Tracer");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = settings.tracerMaterial != null ? settings.tracerMaterial : material; line.positionCount = 2;
            line.SetPosition(0, start); line.SetPosition(1, end);
            line.startColor = line.endColor = settings.tracerColor;
            line.startWidth = line.endWidth = width;
            Destroy(go, lifetime);
        }
    }
}
