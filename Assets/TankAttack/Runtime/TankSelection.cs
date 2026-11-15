using UnityEngine;
namespace TankAttack
{
    public static class TankSelection
    {
        public static TankDefinition Selected { get; private set; }
        public static void Select(TankDefinition tank) => Selected = tank;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => Selected = null;
    }
}
