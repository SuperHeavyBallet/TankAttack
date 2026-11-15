using UnityEngine;
namespace TankAttack
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ExplosionLifetime : MonoBehaviour
    {
        ParticleSystem particles;
        void Awake() => particles=GetComponent<ParticleSystem>();
        void Update() { if (!particles.IsAlive(true)) Destroy(gameObject); }
    }
}
