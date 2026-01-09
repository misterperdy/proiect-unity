using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.AI.Navigation;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class DungeonGenerator : MonoBehaviour
{
    public static DungeonGenerator instance;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public enum DoorSide { Top, Right, Bottom, Left }

    [Header("Teleporters")]
    public GameObject teleporterPrefab;

    [System.Serializable]
    public struct BiomeConfig
    {
        public string name;
        public int roomCount;
        public Material floorMaterial;
        public Material wallMaterial;

        [Header("Boss")]
        public GameObject bossPrefab;

        [Header("Enemies")]
        public List<GameObject> enemyPrefabs;
        public float statMultiplier; // e.g. 1.0 for Biome 1, 1.5 for Biome 2

        [Header("Enemy Settings")]
        public int minEnemies; 
        public int maxEnemies;
    }

    [System.Serializable]
    public class LevelData
    {
        public int[,] grid;
        public Vector3 worldOffset;
        public List<Vector2Int> discoveredTiles;
    }

    [Header("Levels History")]
    public List<LevelData> levelHistory = new List<LevelData>();

    [Header("Enemy Rarity Settings")]
    public RoomEnemySpawner.RaritySettings[] rarityDefinitions;

    [Header("Spawn Settings")]
    public int biomeSpawnIndex = 0;
    public float distanceDifficultyFactor = 0.05f;

    [Header("Grid Settings")]
    public int gridSize = 250; // Increased for 40+ rooms
    public float tileSize = 9.5f;
    public Vector3 levelDistanceOffset = new Vector3(0, 0, 1000);

    [Header("Biomes & Progression")]
    public List<BiomeConfig> biomes;
    public int currentBiomeIndex = 0;

    [Header("Player")]
    public Transform player;

    [Header("Weapons")]
    public GameObject meleeWeapon;
    public GameObject rangedWeapon;

    [Header("Start Room")]
    public GameObject startRoomPrefab;
    public int startRoomWidth = 5;
    public int startRoomHeight = 2;
    public Vector2Int startRoomDoorOffset = new Vector2Int(2, 2);

    [Header("Boss Room")]
    public GameObject bossRoomPrefab;
    public int bossRoomWidth = 5;  
    public int bossRoomHeight = 5; 
    public Vector2Int bossEntryOffset = new Vector2Int(2, 0);

    [Header("Standard Rooms")]
    public RoomGenerator roomGeneratorPrefab;
    public int minRoomSize = 3;
    public int maxRoomSize = 6;

    [Header("Generation Logic")]
    [Range(0f, 1f)] public float layerSpawnChance = 0.6f;

    [Header("Rotation Logic")]
    public DoorSide prefabDefaultDoorSide = DoorSide.Top;
    [Range(0, 270)] public int globalRotationFix = 0;

    [Header("Adjustments")]
    public float roomVisualNudgeX = 0f;
    public float roomVisualNudgeZ = 0f;

    [Header("Prefabs")]
    public GameObject straightHallway;
    public GameObject cornerHallway;
    public GameObject tJunction;
    public GameObject crossIntersection;

    [Header("Container")]
    public Transform dungeonContainer;

    [Header("NavMesh")]
    public NavMeshSurface navMeshSurface;

    [Header("UI")]
    public MinimapController minimapController; //asign in inspector

    [Header("Debug")]
    public Vector3 currentBossPosition;

    // --- Internal State ---
    private int[,] grid;
    private List<Vector2Int> roomConnectionPoints = new List<Vector2Int>();
    private Vector3 currentWorldOffset = Vector3.zero;
    private int[,] tileBiomeMap;
    private Vector2Int startRoomCenter;

    struct DoorInfo
    {
        public Vector2Int gridPos;
        public Vector2Int stepPos;
        public Vector2Int dir;
    }

    void Start()
    {
        if (navMeshSurface == null) navMeshSurface = GetComponent<NavMeshSurface>();

        GenerateCurrentLevel();
    }

    public void LoadLevelMap(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levelHistory.Count)
        {
            LevelData data = levelHistory[levelIndex];

            if (minimapController != null)
            {
                minimapController.InitializeMinimap(data.grid, gridSize, tileSize, data.worldOffset);
                minimapController.RestoreDiscoveredTiles(data.discoveredTiles);
            }
        }
    }

    public Vector3 GenerateNextLevel(Vector3 positionToReturnTo, bool updateMinimap = false)
    {
        currentBiomeIndex++;
        if (currentBiomeIndex >= biomes.Count) currentBiomeIndex = biomes.Count - 1;

        currentWorldOffset += levelDistanceOffset;

        GenerateCurrentLevel(updateMinimap);

        float cx = ((startRoomWidth - 1) * tileSize) / 2f;
        float cy = ((startRoomHeight - 1) * tileSize) / 2f;

        int x = gridSize / 2 - (startRoomWidth / 2);
        int y = gridSize / 2 - (startRoomHeight / 2);

        Vector3 newStartPos = new Vector3((x * tileSize) + cx, 0, (y * tileSize) + cy) + currentWorldOffset;

        if (teleporterPrefab != null && positionToReturnTo != Vector3.zero)
        {
            GameObject backPortal = Instantiate(teleporterPrefab, newStartPos + new Vector3(0f, 0.3f, 0f) + Vector3.back * 3, Quaternion.identity);
            backPortal.name = "Teleporter_Back";
            backPortal.GetComponent<TeleporterBoss>().SetDestination(positionToReturnTo);
            backPortal.GetComponent<TeleporterBoss>().targetLevelIndex = currentBiomeIndex - 1;
        }

        return newStartPos;
    }

    void GenerateCurrentLevel(bool updateMinimap = true)
    {
        grid = new int[gridSize, gridSize];
        tileBiomeMap = new int[gridSize, gridSize];


        roomConnectionPoints.Clear();

        for (int x = 0; x < gridSize; x++)
            for (int y = 0; y < gridSize; y++)
                tileBiomeMap[x, y] = -1;

        GameObject levelObj = new GameObject("Level_" + currentBiomeIndex);
        if (dungeonContainer != null) levelObj.transform.parent = dungeonContainer;

        Vector2Int center = new Vector2Int(gridSize / 2, gridSize / 2);

        BiomeConfig currentBiome = biomes[currentBiomeIndex];

        PlaceStartRoom(center, levelObj.transform, currentBiome);

        List<Vector2Int> biomeRooms = PlaceBiomeRooms(currentBiome, currentBiomeIndex, center, levelObj.transform);

        ConnectSpecificRooms(biomeRooms);

        Vector2Int furthest = GetFurthestRoom(biomeRooms, center);
        PlaceBossGate(furthest, currentBiomeIndex, levelObj.transform);

        SpawnWorld(levelObj.transform, currentBiome);

        //init minimap
        if (updateMinimap && minimapController != null)
        {
            minimapController.playerTransform = player;
            minimapController.InitializeMinimap(grid, gridSize, tileSize, currentWorldOffset);
        }
        else
        {
            Debug.LogWarning("minimap controller is null on dungeon script");
        }

        LevelData newData = new LevelData();
        newData.grid = grid;
        newData.worldOffset = currentWorldOffset;
        newData.discoveredTiles = new List<Vector2Int>();

        if (levelHistory.Count <= currentBiomeIndex)
        {
            levelHistory.Add(newData);
        }
        else
        {
            levelHistory[currentBiomeIndex] = newData;
        }

        if (navMeshSurface != null)
        {
            // Update the NavMeshSurface to point to the container if needed, 
            // or ensure the Generator is the parent of the container.
            navMeshSurface.BuildNavMesh();
        }
    }

    public void SaveExplorationForLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levelHistory.Count && minimapController != null)
        {
            levelHistory[levelIndex].discoveredTiles = minimapController.GetDiscoveredTiles();
        }
    }



    void PlaceStartRoom(Vector2Int center, Transform levelParent, BiomeConfig theme)
    {
        int x = center.x - (startRoomWidth / 2);
        int y = center.y - (startRoomHeight / 2);
        startRoomCenter = new Vector2Int(x + startRoomWidth / 2, y + startRoomHeight / 2);

        DoorInfo door = GetRotatedDoorInfo(x, y, startRoomWidth, startRoomHeight, 180, startRoomDoorOffset);

        if (!IsInsideGrid(door.stepPos)) return;

        MarkPadding(x, y, startRoomWidth, startRoomHeight);
        MarkRoom(x, y, startRoomWidth, startRoomHeight, 0);

        grid[door.stepPos.x, door.stepPos.y] = 3;
        tileBiomeMap[door.stepPos.x, door.stepPos.y] = 0;
        roomConnectionPoints.Add(door.stepPos);

        ClearEntryPoint(door.stepPos, door.dir);

        float cx = ((startRoomWidth - 1) * tileSize) / 2f;
        float cy = ((startRoomHeight - 1) * tileSize) / 2f;
        Vector3 finalPos = new Vector3((x * tileSize) + cx, 0, (y * tileSize) + cy) + currentWorldOffset;

        GameObject startRoomObj = Instantiate(startRoomPrefab, finalPos, Quaternion.identity, levelParent);
        ApplyThemeRecursively(startRoomObj, theme);
        Instantiate(meleeWeapon, finalPos + Vector3.up + Vector3.right*3, Quaternion.identity, levelParent);
        Instantiate(rangedWeapon, finalPos + Vector3.up + Vector3.left*3, Quaternion.identity, levelParent);

        if (player != null && currentBiomeIndex==biomeSpawnIndex)
        {
            player.position = finalPos + new Vector3(0, 2, 0);
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null) { playerRb.velocity = Vector3.zero; playerRb.position = player.position; }
            player.rotation = Quaternion.identity;
        }
    }

    List<Vector2Int> PlaceBiomeRooms(BiomeConfig biome, int biomeIndex, Vector2Int center, Transform levelParent)
    {
        List<Vector2Int> placedRooms = new List<Vector2Int>();
        placedRooms.Add(center);

        int roomsPlaced = 0;
        int targetRooms = biome.roomCount;

        // Increased Loop Limit happens here
        List<Vector2Int> spiralPoints = GenerateSpiralPoints(center);

        for (int i = 1; i < spiralPoints.Count; i++)
        {
            if (roomsPlaced >= targetRooms) break;

            Vector2Int pos = spiralPoints[i];

            bool forceFirst = (roomsPlaced == 0);
            if (Random.value < layerSpawnChance || forceFirst)
            {
                int randW = Random.Range(minRoomSize, maxRoomSize + 1);
                int randH = Random.Range(minRoomSize, maxRoomSize + 1);
                if (randW % 2 == 0) randW = (Random.Range(0, 2) % 2 == 0) ? randW - 1 : randW + 1;

                if (TryPlaceRoom(pos.x, pos.y, randW, randH, center, biome, biomeIndex, levelParent))
                {
                    float rot = CalculateRotationTowardsTarget(pos.x, pos.y, randW, randH, center);

                    int dx = randW / 2;
                    int dy = 0;

                    Vector2Int dp = GetRotatedDoorInfo(pos.x, pos.y, randW, randH, rot, new Vector2Int(dx, dy)).stepPos;

                    placedRooms.Add(dp);
                    roomsPlaced++;
                }
            }
        }
        return placedRooms;
    }

    bool TryPlaceRoom(int x, int y, int w, int h, Vector2Int target, BiomeConfig theme, int biomeIndex, Transform levelParent)
    {
        float rotation = CalculateRotationTowardsTarget(x, y, w, h, target);

        int doorIndexX = w / 2;
        int doorIndexY = 0;
        Vector2Int localDoorPos = new Vector2Int(doorIndexX, doorIndexY);

        int gridDy = doorIndexY;
        if (prefabDefaultDoorSide == DoorSide.Top) gridDy += 1;
        else if (prefabDefaultDoorSide == DoorSide.Bottom) gridDy -= 1;

        int occupiedW = w; int occupiedH = h;
        bool isSideways = Mathf.Approximately(Mathf.Abs(rotation - 90), 0) || Mathf.Approximately(Mathf.Abs(rotation - 270), 0);
        if (isSideways) { occupiedW = h; occupiedH = w; }

        if (!IsInsideGrid(new Vector2Int(x, y))) return false;
        if (x < 4 || y < 4 || x > gridSize - occupiedW - 4 || y > gridSize - occupiedH - 4) return false;

        if (IsAreaClear(x, y, occupiedW, occupiedH))
        {
            DoorInfo door = GetRotatedDoorInfo(x, y, w, h, rotation, new Vector2Int(doorIndexX, gridDy));

            if (!IsInsideGrid(door.stepPos) || grid[door.stepPos.x, door.stepPos.y] != 0) return false;

            MarkPadding(x, y, occupiedW, occupiedH);
            MarkRoom(x, y, occupiedW, occupiedH, biomeIndex);

            grid[door.stepPos.x, door.stepPos.y] = 3;
            tileBiomeMap[door.stepPos.x, door.stepPos.y] = biomeIndex;

            ClearEntryPoint(door.stepPos, door.dir);

            float midX = x + (occupiedW - 1) / 2f;
            float midY = y + (occupiedH - 1) / 2f;
            Vector3 finalPos = new Vector3(midX * tileSize, 0, midY * tileSize) + currentWorldOffset;

            if (isSideways) { finalPos.x += roomVisualNudgeZ; finalPos.z += roomVisualNudgeX; }
            else { finalPos.x += roomVisualNudgeX; finalPos.z += roomVisualNudgeZ; }

            GameObject roomObj = Instantiate(roomGeneratorPrefab.gameObject, finalPos, Quaternion.Euler(0, rotation, 0), levelParent);

            roomObj.GetComponent<RoomGenerator>().BuildRoom(w, h, localDoorPos, theme.floorMaterial, theme.wallMaterial);

            RoomEnemySpawner spawner = roomObj.AddComponent<RoomEnemySpawner>();

            float dist = Vector2.Distance(startRoomCenter, new Vector2(x, y));
            float diffMult = 1.0f + (dist * distanceDifficultyFactor);

            spawner.Initialize(
                theme.enemyPrefabs,
                diffMult,
                theme.statMultiplier,
                rarityDefinitions,
                theme.minEnemies,
                theme.maxEnemies,
                w,      
                h,     
                tileSize
            );


            return true;
        }
        return false;
    }

    void PlaceBossGate(Vector2Int originPoint, int biomeIndex, Transform levelParent)
    {
        List<Vector2Int> candidates = GenerateSpiralPoints(originPoint);

        foreach (Vector2Int pos in candidates)
        {
            if (Vector2Int.Distance(pos, originPoint) < 6) continue;

            // Updated Area Check with new Boss Width/Height
            if (IsAreaClear(pos.x, pos.y, bossRoomWidth, bossRoomHeight))
            {
                MarkPadding(pos.x, pos.y, bossRoomWidth, bossRoomHeight);
                MarkRoom(pos.x, pos.y, bossRoomWidth, bossRoomHeight, biomeIndex);

                Vector2Int entry = pos + bossEntryOffset;
                if (IsInsideGrid(entry))
                {
                    grid[entry.x, entry.y] = 3;
                    tileBiomeMap[entry.x, entry.y] = biomeIndex;
                }

                // Connect
                ApplyPathToGrid(FindPath(originPoint, entry), biomeIndex);

                // Visuals
                float cx = ((bossRoomWidth - 1) * tileSize) / 2f;
                float cy = ((bossRoomHeight - 1) * tileSize) / 2f;
                Vector3 finalPos = new Vector3((pos.x * tileSize) + cx, 0, (pos.y * tileSize) + cy) + currentWorldOffset;

                currentBossPosition = finalPos;

                GameObject bossObj = Instantiate(bossRoomPrefab, finalPos, Quaternion.identity, levelParent);

                ApplyThemeRecursively(bossObj, biomes[biomeIndex]);

                BossRoomSpawner spawner = bossObj.AddComponent<BossRoomSpawner>();

                spawner.Initialize(
                    biomes[biomeIndex].bossPrefab,
                    bossRoomWidth,
                    bossRoomHeight,
                    tileSize
                );


                return;
            }
        }

        Debug.LogWarning("Could not place Boss Room!");
    }

    void SpawnWorld(Transform levelParent, BiomeConfig biome)
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y] == 2 || grid[x, y] == 3)
                    SpawnTile(x, y, levelParent, biome);
            }
        }
    }

    void SpawnTile(int x, int y, Transform levelParent, BiomeConfig biome)
    {
        if (grid[x, y] == 3) return;
        bool n = IsPath(x, y + 1); bool s = IsPath(x, y - 1); bool e = IsPath(x + 1, y); bool w = IsPath(x - 1, y);
        GameObject prefab = null; float rotation = 0;
        int count = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);

        if (count >= 3)
        {
            prefab = (count == 4) ? crossIntersection : tJunction;
            if (count == 3) { if (!n) rotation = 0; else if (!e) rotation = 90; else if (!s) rotation = 180; else if (!w) rotation = 270; }
        }
        else if (n && s) { prefab = straightHallway; rotation = 0; }
        else if (e && w) { prefab = straightHallway; rotation = 90; }
        else
        {
            prefab = cornerHallway;
            if (s && e) rotation = 0; if (s && w) rotation = 90; if (n && w) rotation = 180; if (n && e) rotation = 270;
            if (count == 1) { prefab = straightHallway; if (n || s) rotation = 0; else rotation = 90; }
        }

        if (prefab != null)
        {
            Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize) + currentWorldOffset;
            GameObject hall = Instantiate(prefab, pos, Quaternion.Euler(0, rotation, 0), levelParent);
            ApplyHallwayTheme(hall, biome);
        }
    }

    void ApplyThemeRecursively(GameObject obj, BiomeConfig theme)
    {
        if (theme.floorMaterial == null || theme.wallMaterial == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            //Renderer[] ChildRenderers = r.GetComponentsInChildren<Renderer>();
            // Only paint parts we are sure about, preserving Prop colors
            if (r.gameObject.name.Contains("Floor"))
            {
                r.material = theme.floorMaterial;
            }
            else if (r.gameObject.name.Contains("Wall") || r.gameObject.name.Contains("Column") || r.gameObject.name.Contains("Pillar"))
            {
                r.material = theme.wallMaterial;
            }
        }
    }

    void ApplyHallwayTheme(GameObject obj, BiomeConfig theme)
    {
        if (theme.floorMaterial == null || theme.wallMaterial == null) return;
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            if (r.gameObject.name.Contains("Floor")) r.material = theme.floorMaterial;
            else r.material = theme.wallMaterial;
        }
    }

    void ConnectSpecificRooms(List<Vector2Int> points)
    {
        if (points.Count < 2) return;

        List<Vector2Int> sorted = new List<Vector2Int>();
        List<Vector2Int> pool = new List<Vector2Int>(points);
        Vector2Int current = pool[0];
        sorted.Add(current);
        pool.RemoveAt(0);

        while (pool.Count > 0)
        {
            Vector2Int closest = Vector2Int.zero;
            float minDst = float.MaxValue;
            int closestIndex = -1;
            for (int i = 0; i < pool.Count; i++)
            {
                float dst = Vector2Int.Distance(current, pool[i]);
                if (dst < minDst) { minDst = dst; closest = pool[i]; closestIndex = i; }
            }
            sorted.Add(closest);
            current = closest;
            pool.RemoveAt(closestIndex);
        }

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            ApplyPathToGrid(FindPath(sorted[i], sorted[i + 1]), currentBiomeIndex);
        }
    }

    void ApplyPathToGrid(List<Vector2Int> path, int biomeIndex)
    {
        if (path == null) return;
        foreach (Vector2Int pos in path)
        {
            if (grid[pos.x, pos.y] != 3)
            {
                grid[pos.x, pos.y] = 2;
                tileBiomeMap[pos.x, pos.y] = biomeIndex;
            }
        }
    }

    Vector2Int GetFurthestRoom(List<Vector2Int> rooms, Vector2Int center)
    {
        Vector2Int furthest = center;
        float maxDist = 0;
        foreach (var r in rooms)
        {
            float d = Vector2Int.Distance(center, r);
            if (d > maxDist) { maxDist = d; furthest = r; }
        }
        return furthest;
    }

    // --- MATH & HELPERS (Your Logic) ---

    DoorInfo GetRotatedDoorInfo(int x, int y, int w, int h, float rotation, Vector2Int localDoor)
    {
        int rot = Mathf.RoundToInt(rotation) % 360;
        if (rot < 0) rot += 360;
        Vector2Int internalPos = Vector2Int.zero; Vector2Int direction = Vector2Int.zero;
        switch (rot)
        {
            case 0: internalPos = new Vector2Int(x + (w - 1 - localDoor.x), y + 1); direction = new Vector2Int(0, -1); break;
            case 90: internalPos = new Vector2Int(x + 1, y + localDoor.x); direction = new Vector2Int(-1, 0); break;
            case 180: internalPos = new Vector2Int(x + localDoor.x, y + (h - 2)); direction = new Vector2Int(0, 1); break;
            case 270: internalPos = new Vector2Int(x + (h - 2), y + (w - 1 - localDoor.x)); direction = new Vector2Int(1, 0); break;
        }
        DoorInfo info; info.gridPos = internalPos; info.stepPos = internalPos + direction; info.dir = direction; return info;
    }

    float CalculateRotationTowardsTarget(int x, int y, int w, int h, Vector2Int target)
    {
        Vector2 roomCenter = new Vector2(x + w / 2f, y + h / 2f);
        Vector2 dir = (Vector2)target - roomCenter;
        float targetAngle = 0;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) { if (dir.x > 0) targetAngle = 270; else targetAngle = 90; }
        else { if (dir.y > 0) targetAngle = 180; else targetAngle = 0; }
        float initialAngle = 0;
        switch (prefabDefaultDoorSide)
        {
            case DoorSide.Top: initialAngle = 0; break;
            case DoorSide.Right: initialAngle = 90; break;
            case DoorSide.Bottom: initialAngle = 180; break;
            case DoorSide.Left: initialAngle = 270; break;
        }
        float finalRot = targetAngle - initialAngle;
        finalRot += globalRotationFix;
        return (finalRot + 360) % 360;
    }

    Vector2Int CalculateClearanceDir(float rotation)
    {
        Vector3 defaultDir = Vector3.zero;
        switch (prefabDefaultDoorSide) { case DoorSide.Top: defaultDir = new Vector3(0, 0, 1); break; case DoorSide.Bottom: defaultDir = new Vector3(0, 0, -1); break; case DoorSide.Right: defaultDir = new Vector3(1, 0, 0); break; case DoorSide.Left: defaultDir = new Vector3(-1, 0, 0); break; }
        Vector3 rotatedDir = Quaternion.Euler(0, rotation, 0) * defaultDir; return new Vector2Int(Mathf.RoundToInt(rotatedDir.x), Mathf.RoundToInt(rotatedDir.z));
    }

    List<Vector2Int> GenerateSpiralPoints(Vector2Int center)
    {
        List<Vector2Int> points = new List<Vector2Int>();
        int x = center.x; int y = center.y;
        int stepSize = 1; int stepsTaken = 0; int directionIndex = 0;
        Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(0, 1) };
        points.Add(new Vector2Int(x, y));

        // FIX: Increased search limit
        for (int i = 0; i < 10000; i++)
        {
            x += dirs[directionIndex].x; y += dirs[directionIndex].y;
            if (IsInsideGrid(new Vector2Int(x, y))) points.Add(new Vector2Int(x, y));
            stepsTaken++;
            if (stepsTaken == stepSize)
            {
                stepsTaken = 0; directionIndex = (directionIndex + 1) % 4;
                if (directionIndex % 2 == 0) stepSize++;
            }
        }
        return points;
    }

    bool IsInsideGrid(Vector2Int p) { return p.x > 0 && p.y > 0 && p.x < gridSize - 1 && p.y < gridSize - 1; }
    void MarkPadding(int x, int y, int w, int h) { for (int i = -1; i <= w; i++) for (int j = -1; j <= h; j++) if (IsInsideGrid(new Vector2Int(x + i, y + j))) grid[x + i, y + j] = 4; }
    void MarkRoom(int x, int y, int w, int h, int biomeIndex)
    {
        for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) { grid[x + i, y + j] = 1; tileBiomeMap[x + i, y + j] = biomeIndex; }
    }
    void ClearEntryPoint(Vector2Int doorPos, Vector2Int dir) { for (int i = 0; i < 2; i++) { Vector2Int entry = doorPos + (dir * i); if (IsInsideGrid(entry) && grid[entry.x, entry.y] == 4) grid[entry.x, entry.y] = 0; } }
    bool IsAreaClear(int startX, int startY, int w, int h) { for (int x = startX - 4; x < startX + w + 4; x++) for (int y = startY - 4; y < startY + h + 4; y++) if (!IsInsideGrid(new Vector2Int(x, y)) || grid[x, y] != 0) return false; return true; }
    void SortConnectionPointsByDistance() { /* Redundant but safe */ }
    void ConnectRoomsWithAStar() { /* Redundant but safe */ }
    List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        List<Vector2Int> path = new List<Vector2Int>(); Queue<Vector2Int> queue = new Queue<Vector2Int>(); Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        queue.Enqueue(start); cameFrom[start] = start; int safe = 0;
        while (queue.Count > 0 && safe < 10000)
        {
            safe++; Vector2Int curr = queue.Dequeue(); if (curr == target) { while (curr != start) { path.Add(curr); curr = cameFrom[curr]; } path.Reverse(); return path; }
            Vector2Int[] dirs = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
            foreach (Vector2Int d in dirs) { Vector2Int n = curr + d; if (!IsInsideGrid(n)) continue; bool blocked = (grid[n.x, n.y] == 1 || grid[n.x, n.y] == 4); if (n == target) blocked = false; if (!blocked && !cameFrom.ContainsKey(n)) { queue.Enqueue(n); cameFrom[n] = curr; } }
        }
        return null;
    }
    bool IsPath(int x, int y) { if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) return false; return grid[x, y] == 2 || grid[x, y] == 3; }
}