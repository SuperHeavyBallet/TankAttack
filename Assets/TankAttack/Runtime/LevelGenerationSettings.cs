using UnityEngine;
namespace TankAttack
{
    [CreateAssetMenu(menuName = "Tank Attack/Level Generation Settings")]
    public sealed class LevelGenerationSettings : ScriptableObject
    {
        [Header("Dimensions in meters")]
        [Min(50)] public float minimumLength = 50;
        [Min(50)] public float maximumLength = 300;
        [Min(50)] public float firstLevelMaximumLength = 75;
        [Min(0)] public float lengthGrowthPerLevel = 25;
        [Min(50)] public float minimumWidth = 50;
        [Min(50)] public float maximumWidth = 200;
        [Range(.25f, 1)] public float preferredWidthRatio = .65f;
        [Header("Content per complete length band (minimum one band)")]
        [Min(30)] public float bandLength = 50;
        [Min(0)] public int terrainBlocksPerBand = 4;
        [Tooltip("One of each prefab is placed in each band.")]
        public GameObject[] weaponPickups;
        public GameObject[] terrainPrefabs;
        [Header("Clearances and navigation")]
        [Min(5)] public float terrainClearance = 6;
        [Min(6)] public float centerRouteWidth = 8;
        [Min(2)] public float navigationAgentRadius = 2.3f;
        [Min(10)] public float endClearance = 10;
        [Min(10)] public int placementAttempts = 80;
        [Min(2)] public float wallHeight = 5;
        [Min(1)] public float wallThickness = 2;
        public Material groundMaterial, wallMaterial;
        [Header("Reproduction")]
        [Tooltip("Zero chooses a fresh seed when deploying. Restart keeps the same map.")]
        public int fixedSeed;

        public Vector2 Dimensions(int level, System.Random random)
        {
            float minimum = Mathf.Max(50, minimumLength);
            float maximum = Mathf.Max(minimum, maximumLength);
            float growth = Mathf.Max(0, lengthGrowthPerLevel) * Mathf.Max(0, level - 1);
            float upper = Mathf.Clamp(firstLevelMaximumLength + growth, minimum, maximum);
            float lower = Mathf.Min(upper, minimum + growth * .5f);
            float length = Mathf.Lerp(lower, upper, (float)random.NextDouble());
            float minWidth = Mathf.Min(length, Mathf.Max(50, minimumWidth));
            float maxWidth = Mathf.Clamp(length * preferredWidthRatio, minWidth, Mathf.Max(minWidth, maximumWidth));
            return new Vector2(Mathf.Min(length, Mathf.Lerp(minWidth, maxWidth, (float)random.NextDouble())), length);
        }
        public int BandCount(float length) => Mathf.Max(1, Mathf.FloorToInt(length / Mathf.Max(30, bandLength)));
    }
}
