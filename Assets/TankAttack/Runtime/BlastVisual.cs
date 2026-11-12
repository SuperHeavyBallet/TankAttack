using UnityEngine;
namespace TankAttack
{
    public sealed class BlastVisual : MonoBehaviour
    {
        LineRenderer ring;
        float radius, age;
        public static void Show(Vector3 position, WeaponSettings weapon)
        {
            var go = new GameObject("Blast radius"); go.transform.position = position;
            var visual = go.AddComponent<BlastVisual>(); visual.radius = weapon.blastRadius;
            visual.ring = go.AddComponent<LineRenderer>(); visual.ring.useWorldSpace = false;
            visual.ring.loop = true; visual.ring.positionCount = 40;
            visual.ring.sharedMaterial = weapon.tracerMaterial;
            visual.ring.startWidth = visual.ring.endWidth = .15f;
        }
        void Update()
        {
            age += Time.deltaTime;
            float progress = Mathf.Clamp01(age / .3f);
            for (int i=0;i<ring.positionCount;i++)
            {
                float angle = i * Mathf.PI * 2 / ring.positionCount;
                ring.SetPosition(i,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)) * radius * progress);
            }
            ring.startWidth = ring.endWidth = .15f * (1-progress);
            if (progress >= 1) Destroy(gameObject);
        }
    }
}
