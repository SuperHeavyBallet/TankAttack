using UnityEngine;
namespace TankAttack
{
    [CreateAssetMenu(menuName = "Tank Attack/Player Tank")]
    public sealed class TankDefinition : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public VehicleSettings vehicle;
        public WeaponSettings cannon;
        public EngineAudioSettings engineAudio;
        public Color accent = Color.white;
    }
}
