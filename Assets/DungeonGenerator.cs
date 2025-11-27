using System.Collections;
using System.Collections.Generic;
using System.Linq; // Needed for Grouping and Lists
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public enum DoorSide { Bottom, Top, Left, Right }

    [Header("Grid Settings")]
    public int gridSize = 60;
    public float tileSize = 10f;

    [Header("Player")]
    public Transform player;

    [Header("Start Room")]
    public GameObject startRoomPrefab;
    public int startRoomWidth = 5;
    public int startRoomHeight = 2;
    public Vector2Int startRoomDoorOffset = new Vector2Int(2, 1);

    [Header("Standard Rooms")]
    public GameObject largeRoomPrefab;
    public int numberOfRooms = 6;
    public int roomWidthInTiles = 4;
    public int roomHeightInTiles = 2;
    public float roomVisualNudgeX = 5.0f;
    public float roomVisualNudgeZ = 0f;
    public Vector2Int standardDoorOffset = new Vector2Int(1, 1);

    [Header("Generation Logic")]
    [Tooltip("Chance to spawn EXTRA rooms in a layer after the first one is placed. 0 = Sparse, 1 = Dense.")]
    [Range(0f, 1f)] public float layerSpawnChance = 0.5f;

    [Header("Rotation Logic")]
    public DoorSide prefabDefaultDoorSide = DoorSide.Top;

    [Header("Prefabs")]
    public GameObject straightHallway;
    public GameObject cornerHallway;
    public GameObject tJunction;
    public GameObject crossIntersection;

    [Header("Container")]
    public Transform dungeonContainer;

    private int[,] grid;
    private List<Vector2Int> roomConnectionPoints = new List<Vector2Int>();
    private List<Vector2Int> debugPath = new List<Vector2Int>();
    private Vector2Int startRoomCenter;

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        grid = new int[gridSize, gridSize];
        roomConnectionPoints.Clear();
        debugPath.Clear();

        if (dungeonContainer != null)
            foreach (Transform child in dungeonContainer) Destroy(child.gameObject);

        PlaceStartRoom();
        PlaceRoomsSpiralLayers(); // <-- UPDATED FUNCTION
        SortConnectionPointsByDistance();
        ConnectRoomsWithAStar();
        SpawnWorld();
    }

    void PlaceStartRoom()
    {
        int x = gridSize / 2 - (startRoomWidth / 2);
        int y = gridSize / 2 - (startRoomHeight / 2);
        startRoomCenter = new Vector2Int(x + startRoomWidth / 2, y + startRoomHeight / 2);

        MarkPadding(x, y, startRoomWidth, startRoomHeight);
        MarkRoom(x, y, startRoomWidth, startRoomHeight);

        Vector2Int doorPos = new Vector2Int(x + startRoomDoorOffset.x, y + startRoomDoorOffset.y);
        grid[doorPos.x, doorPos.y] = 3;
        roomConnectionPoints.Add(doorPos);

        ClearEntryPoint(doorPos, new Vector2Int(0, 1));

        float cx = ((startRoomWidth - 1) * tileSize) / 2f;
        float cy = ((startRoomHeight - 1) * tileSize) / 2f;
        Vector3 finalPos = new Vector3((x * tileSize) + cx, 0, (y * tileSize) + cy);

        Instantiate(startRoomPrefab, finalPos, Quaternion.identity, dungeonContainer);
        if (player != null)
        {
            // 1. Teleport the Transform
            player.position = finalPos + new Vector3(0, 2, 0); // Increased to 2 to be safe from floor clipping

            // 2. If the player has a Rigidbody, we must reset its velocity so it doesn't fly away
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.velocity = Vector3.zero;
                playerRb.position = player.position; // Sync physics position
            }

            // 3. Optional: Rotate player to face the room center
            player.rotation = Quaternion.identity;
        }
    }

    // --- NEW "LAYERED" ALGORITHM ---
    void PlaceRoomsSpiralLayers()
    {
        int roomsPlaced = 0;
        int maxRoomsToAdd = numberOfRooms - 1; // -1 because Start Room is already there

        // 1. Get all points
        List<Vector2Int> allSpiralPoints = GenerateSpiralPoints();

        // 2. Group points by their "Ring Distance" from center
        // Dictionary: Ring Index -> List of Coordinates
        Dictionary<int, List<Vector2Int>> layers = new Dictionary<int, List<Vector2Int>>();
        int centerX = gridSize / 2;
        int centerY = gridSize / 2;

        foreach (Vector2Int p in allSpiralPoints)
        {
            // Chebyshev distance (Ring layer index)
            int dist = Mathf.Max(Mathf.Abs(p.x - centerX), Mathf.Abs(p.y - centerY));

            if (!layers.ContainsKey(dist))
                layers.Add(dist, new List<Vector2Int>());

            layers[dist].Add(p);
        }

        // 3. Iterate through Layers (starting from ring 1, moving outward)
        // We order by Key to ensure we go 1, 2, 3...
        foreach (var layerIndex in layers.Keys.OrderBy(k => k))
        {
            if (roomsPlaced >= maxRoomsToAdd) break;
            if (layerIndex == 0) continue; // Skip center (Start Room is there)

            List<Vector2Int> candidates = layers[layerIndex];

            // Shuffle candidates so the "First Room" isn't always in the same direction
            candidates = candidates.OrderBy(x => Random.value).ToList();

            bool placedOneInThisLayer = false;

            // Iterate through candidates in this layer
            foreach (Vector2Int pos in candidates)
            {
                if (roomsPlaced >= maxRoomsToAdd) break;

                // DECISION: Should we try to spawn here?
                // Logic: IF we haven't placed a room in this layer yet -> ALWAYS TRY (Guarantee 1)
                //        ELSE -> Try based on random chance
                bool shouldTry = !placedOneInThisLayer || (Random.value < layerSpawnChance);

                if (shouldTry)
                {
                    if (TryPlaceRoom(pos.x, pos.y))
                    {
                        roomsPlaced++;
                        placedOneInThisLayer = true;
                    }
                }
            }
        }
    }

    // Helper function to handle the actual check and placement logic
    bool TryPlaceRoom(int x, int y)
    {
        if (x < 4 || y < 4 || x > gridSize - roomWidthInTiles - 4 || y > gridSize - roomHeightInTiles - 4) return false;

        if (IsAreaClear(x, y, roomWidthInTiles, roomHeightInTiles))
        {
            float rotation = CalculateRotationTowardsStart(x, y);
            Vector2Int doorGridPos = CalculateRotatedDoorPos(x, y, rotation);
            Vector2Int clearanceDir = CalculateClearanceDir(rotation);

            if (doorGridPos.x < 1 || doorGridPos.y < 1 || doorGridPos.x >= gridSize - 1 || doorGridPos.y >= gridSize - 1) return false;
            if (grid[doorGridPos.x, doorGridPos.y] != 0) return false;

            MarkPadding(x, y, roomWidthInTiles, roomHeightInTiles);
            MarkRoom(x, y, roomWidthInTiles, roomHeightInTiles);

            grid[doorGridPos.x, doorGridPos.y] = 3;
            roomConnectionPoints.Add(doorGridPos);
            ClearEntryPoint(doorGridPos, clearanceDir);

            float midX = x + (roomWidthInTiles - 1) / 2f;
            float midY = y + (roomHeightInTiles - 1) / 2f;
            Vector3 finalPos = new Vector3(midX * tileSize, 0, midY * tileSize);

            Instantiate(largeRoomPrefab, finalPos, Quaternion.Euler(0, rotation, 0), dungeonContainer);
            return true;
        }
        return false;
    }

    // --- ROTATION LOGIC ---

    float CalculateRotationTowardsStart(int x, int y)
    {
        Vector2Int roomCenter = new Vector2Int(x + roomWidthInTiles / 2, y + roomHeightInTiles / 2);
        Vector2Int dir = startRoomCenter - roomCenter;

        float targetGlobalAngle = 0;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (dir.x > 0) targetGlobalAngle = 270; // Face East
            else targetGlobalAngle = 90;            // Face West
        }
        else
        {
            if (dir.y > 0) targetGlobalAngle = 180; // Face North
            else targetGlobalAngle = 0;             // Face South
        }

        float prefabOffset = 0;
        switch (prefabDefaultDoorSide)
        {
            case DoorSide.Bottom: prefabOffset = 0; break;
            case DoorSide.Left: prefabOffset = 90; break;
            case DoorSide.Top: prefabOffset = 180; break;
            case DoorSide.Right: prefabOffset = 270; break;
        }

        float finalRot = targetGlobalAngle - prefabOffset;
        if (finalRot < 0) finalRot += 360;

        return finalRot;
    }

    Vector2Int CalculateRotatedDoorPos(int x, int y, float rotation)
    {
        int w = roomWidthInTiles;
        int h = roomHeightInTiles;
        int idx = standardDoorOffset.x;

        if (Mathf.Approximately(rotation % 180, 0))
        {
            float angle = rotation;
            if (prefabDefaultDoorSide == DoorSide.Top) angle += 180;
            angle = angle % 360;

            if (Mathf.Approximately(angle, 0)) return new Vector2Int(x + idx - 1, y);
            if (Mathf.Approximately(angle, 180)) return new Vector2Int(x + (w - idx), y + h - 1);

            return new Vector2Int(x + idx, y - 1);
        }
        else
        {
            float angle = rotation;
            if (prefabDefaultDoorSide == DoorSide.Top) angle += 180;
            angle = angle % 360;

            if (Mathf.Approximately(angle, 90)) return new Vector2Int(x, y + idx);
            if (Mathf.Approximately(angle, 270)) return new Vector2Int(x + w - 1, y + (h - 1 - idx));

            if (Mathf.Approximately(angle, 0)) return new Vector2Int(x + idx - 1, y - 1);
            return new Vector2Int(x + (w - 1 - idx), y + h);
        }
    }

    Vector2Int CalculateClearanceDir(float rotation)
    {
        float angle = rotation;
        if (prefabDefaultDoorSide == DoorSide.Top) angle += 180;
        angle = angle % 360;

        if (Mathf.Approximately(angle, 0)) return new Vector2Int(0, -1);
        if (Mathf.Approximately(angle, 180)) return new Vector2Int(0, 1);
        if (Mathf.Approximately(angle, 90)) return new Vector2Int(-1, 0);
        if (Mathf.Approximately(angle, 270)) return new Vector2Int(1, 0);
        return new Vector2Int(0, -1);
    }

    // --- HELPERS ---

    List<Vector2Int> GenerateSpiralPoints()
    {
        List<Vector2Int> points = new List<Vector2Int>();
        int x = gridSize / 2; int y = gridSize / 2;
        int stepSize = 1; int stepsTaken = 0; int directionIndex = 0;
        Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(0, 1) };
        points.Add(new Vector2Int(x, y));
        for (int i = 0; i < gridSize * gridSize; i++)
        {
            x += dirs[directionIndex].x; y += dirs[directionIndex].y;
            points.Add(new Vector2Int(x, y));
            stepsTaken++;
            if (stepsTaken == stepSize)
            {
                stepsTaken = 0; directionIndex = (directionIndex + 1) % 4;
                if (directionIndex % 2 == 0) stepSize++;
            }
        }
        return points;
    }

    void MarkPadding(int x, int y, int w, int h)
    {
        for (int i = -1; i <= w; i++) for (int j = -1; j <= h; j++)
                if (x + i >= 0 && x + i < gridSize && y + j >= 0 && y + j < gridSize) grid[x + i, y + j] = 4;
    }
    void MarkRoom(int x, int y, int w, int h)
    {
        for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) grid[x + i, y + j] = 1;
    }
    void ClearEntryPoint(Vector2Int doorPos, Vector2Int dir)
    {
        Vector2Int entry = doorPos + dir;
        if (entry.x >= 0 && entry.x < gridSize && entry.y >= 0 && entry.y < gridSize)
            if (grid[entry.x, entry.y] == 4) grid[entry.x, entry.y] = 0;
    }
    bool IsAreaClear(int startX, int startY, int w, int h)
    {
        for (int x = startX - 4; x < startX + w + 4; x++) for (int y = startY - 4; y < startY + h + 4; y++)
                if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) return false;
                else if (grid[x, y] != 0) return false;
        return true;
    }
    void OnDrawGizmos()
    {
        if (roomConnectionPoints == null) return;
        Gizmos.color = Color.red;
        foreach (var p in roomConnectionPoints) Gizmos.DrawSphere(new Vector3(p.x * tileSize, 2, p.y * tileSize), 2f);
    }
    void SortConnectionPointsByDistance()
    {
        if (roomConnectionPoints.Count < 2) return;
        List<Vector2Int> sorted = new List<Vector2Int>();
        List<Vector2Int> pool = new List<Vector2Int>(roomConnectionPoints);
        Vector2Int current = pool[0]; sorted.Add(current); pool.RemoveAt(0);
        while (pool.Count > 0)
        {
            Vector2Int closest = Vector2Int.zero; float minDst = float.MaxValue; int closestIndex = -1;
            for (int i = 0; i < pool.Count; i++)
            {
                float dst = Vector2Int.Distance(current, pool[i]);
                if (dst < minDst) { minDst = dst; closest = pool[i]; closestIndex = i; }
            }
            sorted.Add(closest); current = closest; pool.RemoveAt(closestIndex);
        }
        roomConnectionPoints = sorted;
    }
    void ConnectRoomsWithAStar()
    {
        for (int i = 0; i < roomConnectionPoints.Count - 1; i++)
        {
            List<Vector2Int> path = FindPath(roomConnectionPoints[i], roomConnectionPoints[i + 1]);
            if (path != null) foreach (Vector2Int pos in path) if (grid[pos.x, pos.y] != 3) grid[pos.x, pos.y] = 2;
        }
    }
    List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        queue.Enqueue(start); cameFrom[start] = start;
        int safe = 0;
        while (queue.Count > 0 && safe < 10000)
        {
            safe++; Vector2Int curr = queue.Dequeue();
            if (curr == target)
            {
                while (curr != start) { path.Add(curr); curr = cameFrom[curr]; }
                path.Reverse(); return path;
            }
            Vector2Int[] dirs = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
            foreach (Vector2Int d in dirs)
            {
                Vector2Int n = curr + d;
                if (n.x < 1 || n.y < 1 || n.x >= gridSize - 1 || n.y >= gridSize - 1) continue;
                bool blocked = (grid[n.x, n.y] == 1 || grid[n.x, n.y] == 4);
                if (n == target) blocked = false;
                if (!blocked && !cameFrom.ContainsKey(n)) { queue.Enqueue(n); cameFrom[n] = curr; }
            }
        }
        return null;
    }
    void SpawnWorld()
    {
        for (int x = 0; x < gridSize; x++) for (int y = 0; y < gridSize; y++)
                if (grid[x, y] == 2 || grid[x, y] == 3) SpawnTile(x, y);
    }
    void SpawnTile(int x, int y)
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
        if (prefab != null) Instantiate(prefab, new Vector3(x * tileSize, 0, y * tileSize), Quaternion.Euler(0, rotation, 0), dungeonContainer);
    }
    bool IsPath(int x, int y)
    {
        if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) return false;
        return grid[x, y] == 2 || grid[x, y] == 3;
    }
}