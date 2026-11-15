using UnityEngine;
namespace TankAttack
{
    [CreateAssetMenu(menuName = "Tank Attack/Tank Roster")]
    public sealed class TankRoster : ScriptableObject
    {
        public TankDefinition[] tanks;
        public string gameplayScene = "Assets/TankAttack/Scenes/TankAttack_Prototype.unity";
        public string menuScene = "Assets/TankAttack/Scenes/TankAttack_Menu.unity";
    }
}
