using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace TankAttack
{
    [DefaultExecutionOrder(-150)]
    public sealed class GeneratedLevel : MonoBehaviour
    {
        public LevelGenerationSettings settings;
        public BattleSession session;
        public EnemySpawner spawner;
        public bool Ready { get; private set; }
        public string Error { get; private set; }
        public float Width { get; private set; }
        public float Length { get; private set; }
        public int LevelNumber { get; private set; }
        public int Seed { get; private set; }
        public Transform Geometry { get; private set; }
        public IReadOnlyList<Bounds> TerrainBounds => terrainBounds;
        public IReadOnlyList<Vector3> PickupPositions => pickupPositions;
        readonly List<Bounds> terrainBounds = new List<Bounds>();
        readonly List<Rect> reserved = new List<Rect>();
        readonly List<Vector3> pickupPositions = new List<Vector3>();
        readonly List<GameObject> pickups = new List<GameObject>();
        NavMeshData navigation;
        NavMeshDataInstance navigationInstance;
        System.Random random;
        Rigidbody playerBody;
        bool playerWasKinematic;

        void Awake()
        {
            spawner.gameObject.SetActive(false);
            playerBody = session.player.GetComponent<Rigidbody>();
            playerWasKinematic = playerBody.isKinematic;
            playerBody.isKinematic = true;
            session.destination.gameObject.SetActive(false);
        }
        IEnumerator Start()
        {
            session.BeginGeneration();
            yield return null; // Give the loading HUD one frame before building geometry.
            try { Build(); }
            catch (Exception exception)
            {
                Error = exception.Message;
                Debug.LogException(exception, this);
                yield break;
            }
            yield return null;
            LevelRun.Restore(session.player);
            playerBody.isKinematic = playerWasKinematic;
            session.destination.gameObject.SetActive(true);
            foreach (var pickup in pickups) pickup.SetActive(true);
            Ready = true;
            session.FinishGeneration(LevelRun.EntryScore);
            spawner.gameObject.SetActive(true);
        }
        void Build()
        {
            if (settings == null) throw new InvalidOperationException("Assign level generation settings.");
            LevelRun.EnsureStarted(settings.fixedSeed);
            LevelNumber = LevelRun.Level;
            Seed = unchecked(LevelRun.Seed + LevelNumber * 7919);
            random = new System.Random(Seed);
            var dimensions = settings.Dimensions(LevelNumber, random);
            Width = dimensions.x; Length = dimensions.y;
            Geometry = new GameObject("Generated geometry").transform; Geometry.SetParent(transform, false);
            float t = settings.wallThickness, h = settings.wallHeight;
            Cube("Floor", new Vector3(0,-.5f,Length*.5f), new Vector3(Width,1,Length), settings.groundMaterial);
            Cube("West wall", new Vector3(-Width*.5f-t*.5f,h*.5f,Length*.5f), new Vector3(t,h,Length+2*t), settings.wallMaterial);
            Cube("East wall", new Vector3(Width*.5f+t*.5f,h*.5f,Length*.5f), new Vector3(t,h,Length+2*t), settings.wallMaterial);
            Cube("Start wall", new Vector3(0,h*.5f,-t*.5f), new Vector3(Width,h,t), settings.wallMaterial);
            Cube("End wall", new Vector3(0,h*.5f,Length+t*.5f), new Vector3(Width,h,t), settings.wallMaterial);
            playerBody.position = new Vector3(0,.68f,7);
            playerBody.rotation = Quaternion.identity;
            // Teleport the rendered transform too; interpolated bodies otherwise retain
            // the template scene's old pose until the next physics step.
            session.player.transform.SetPositionAndRotation(playerBody.position,Quaternion.identity);
            var camera = Camera.main;
            if (camera != null && camera.TryGetComponent<FollowCamera>(out var follow)) camera.transform.position=playerBody.position+follow.offset;
            session.destination.position = new Vector3(0,.035f,Length-7);
            Reserve(new Vector3(0,0,7), 8);
            Reserve(session.destination.position, 9);
            PlaceBandContent();
            PlaceTerrain();
            Physics.SyncTransforms();
            BuildNavigation();
            ValidateRoutes();
        }
        void Cube(string name, Vector3 position, Vector3 size, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name;
            cube.transform.SetParent(Geometry, false); cube.transform.position = position; cube.transform.localScale = size;
            cube.GetComponent<Renderer>().sharedMaterial = material;
        }
        float Between(float a, float b) => Mathf.Lerp(a,b,(float)random.NextDouble());
        void Reserve(Vector3 position, float halfSize) => reserved.Add(new Rect(position.x-halfSize,position.z-halfSize,halfSize*2,halfSize*2));
        void PlaceBandContent()
        {
            int bands = settings.BandCount(Length);
            var points = new List<Transform>();
            for (int band=0; band<bands; band++)
            {
                float start = band*Mathf.Max(30,settings.bandLength), end = Mathf.Min(Length,start+Mathf.Max(30,settings.bandLength));
                float middle = (start+end)*.5f;
                float inset = Mathf.Min(6,(end-start)*.25f);
                float z = Mathf.Clamp(middle+Between(-2,2),start+inset,end-inset);
                for (int side=-1; side<=1; side+=2)
                {
                    var point = new GameObject($"Band {band+1} / {(side<0 ? "West" : "East")} spawn").transform;
                    point.SetParent(spawner.transform, false);
                    point.position = new Vector3(side*Width*Between(.29f,.33f),.68f,z);
                    point.rotation = Quaternion.Euler(0,180,0); points.Add(point); Reserve(point.position,5);
                }
                if (settings.weaponPickups == null) continue;
                for (int i=0;i<settings.weaponPickups.Length;i++)
                {
                    if (settings.weaponPickups[i] == null) throw new InvalidOperationException("A weapon pickup prefab is missing.");
                    float x = (i-(settings.weaponPickups.Length-1)*.5f)*7+Between(-.8f,.8f);
                    var position = new Vector3(x,.5f,Mathf.Clamp(start+(end-start)*.28f+Between(-2,2),start+inset,end-inset));
                    var pickup = Instantiate(settings.weaponPickups[i],position,Quaternion.identity,transform);
                    pickup.name = $"Band {band+1} / {settings.weaponPickups[i].name}";
                    pickup.SetActive(false); pickups.Add(pickup); pickupPositions.Add(position); Reserve(position,4);
                }
            }
            spawner.spawnPoints = points.ToArray();
            spawner.randomizeVariants = true;
            spawner.variantSeed = Seed;
        }
        public float Clearance => Mathf.Max(settings.terrainClearance, settings.navigationAgentRadius*2+.5f);
        void PlaceTerrain()
        {
            if (settings.terrainPrefabs == null || settings.terrainPrefabs.Length == 0) return;
            float route = Mathf.Max(settings.centerRouteWidth,Clearance);
            float ends = Mathf.Max(10,settings.endClearance);
            int bands = settings.BandCount(Length);
            for (int band=0;band<bands;band++)
                for (int i=0;i<Mathf.Max(0,settings.terrainBlocksPerBand);i++)
                {
                    var prefab = settings.terrainPrefabs[random.Next(settings.terrainPrefabs.Length)];
                    if (prefab == null) continue;
                    var candidate = Instantiate(prefab,Geometry);
                    bool placed = false;
                    for (int attempt=0;attempt<Mathf.Max(10,settings.placementAttempts);attempt++)
                    {
                        candidate.transform.SetPositionAndRotation(new Vector3(Between(-Width*.5f,Width*.5f),0,Between(band*Mathf.Max(30,settings.bandLength),Mathf.Min(Length,(band+1)*Mathf.Max(30,settings.bandLength)))), Quaternion.Euler(0,random.Next(4)*90,0));
                        Physics.SyncTransforms();
                        if (!SolidBounds(candidate,out var bounds)) break;
                        candidate.transform.position += Vector3.up * -bounds.min.y;
                        bounds.center += Vector3.up * -bounds.min.y;
                        var rect = new Rect(bounds.min.x,bounds.min.z,bounds.size.x,bounds.size.z);
                        if (rect.xMin < -Width*.5f+Clearance || rect.xMax > Width*.5f-Clearance || rect.yMin < ends || rect.yMax > Length-ends) continue;
                        if (rect.xMin < route*.5f && rect.xMax > -route*.5f) continue;
                        bool overlaps = false;
                        foreach (var space in reserved) if (rect.Overlaps(space)) { overlaps=true; break; }
                        foreach (var other in terrainBounds)
                        {
                            var expanded = new Rect(other.min.x-Clearance,other.min.z-Clearance,other.size.x+Clearance*2,other.size.z+Clearance*2);
                            if (rect.Overlaps(expanded)) { overlaps=true; break; }
                        }
                        if (overlaps) continue;
                        terrainBounds.Add(bounds); placed=true; break;
                    }
                    if (!placed) { candidate.SetActive(false); Destroy(candidate); }
                }
        }
        static bool SolidBounds(GameObject item, out Bounds bounds)
        {
            bounds = default; bool found = false;
            foreach (var collider in item.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger) continue;
                if (!found) { bounds=collider.bounds; found=true; } else bounds.Encapsulate(collider.bounds);
            }
            return found;
        }
        void BuildNavigation()
        {
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(Geometry,~0,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
            var build = NavMesh.GetSettingsByID(0);
            build.agentRadius = settings.navigationAgentRadius; build.agentHeight=2; build.agentClimb=.3f; build.agentSlope=35;
            build.overrideVoxelSize=true; build.voxelSize=.2f;
            navigation = NavMeshBuilder.BuildNavMeshData(build,sources,new Bounds(new Vector3(0,3,Length*.5f),new Vector3(Width+10,20,Length+10)),Vector3.zero,Quaternion.identity);
            if (navigation == null) throw new InvalidOperationException("Generated navigation could not be built.");
            navigationInstance = NavMesh.AddNavMeshData(navigation);
        }
        void ValidateRoutes()
        {
            if (!Reachable(session.destination.position)) throw new InvalidOperationException($"Extraction has no complete route: start {playerBody.position}, end {session.destination.position}, map {Width} x {Length}.");
            foreach (var point in spawner.spawnPoints)
                if (!Reachable(point.position)) throw new InvalidOperationException("A spawn zone has no complete route.");
            foreach (var point in pickupPositions)
                if (!Reachable(point)) throw new InvalidOperationException("A pickup has no complete route.");
        }
        public bool Reachable(Vector3 position)
        {
            var path = new NavMeshPath();
            return NavMesh.SamplePosition(playerBody.position,out var start,3,NavMesh.AllAreas)
                && NavMesh.SamplePosition(position,out var end,3,NavMesh.AllAreas)
                && NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)
                && path.status == NavMeshPathStatus.PathComplete;
        }
        void OnDestroy()
        {
            if (navigationInstance.valid) navigationInstance.Remove();
            if (navigation != null) Destroy(navigation);
        }
    }
}
