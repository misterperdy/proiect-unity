using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    // Simple enum to help us visualize which side the door is on
    public enum DoorSide { Top, Right, Bottom, Left }

    [Header("Grid Settings")]
    public int gridSize = 60; // How big the total map is
    public float tileSize = 9.5f; // Matches the physical size of the room assets

    [Header("Player")]
    public Transform player; // We move this object to the start room when done

    [Header("Start Room")]
    public GameObject startRoomPrefab;
    public int startRoomWidth = 5;
    public int startRoomHeight = 2;
    // We set this to 2, 2 because the room is height 2, so Y=2 is just outside the top wall
    public Vector2Int startRoomDoorOffset = new Vector2Int(2, 2);

    [Header("Standard Rooms")]
    public RoomGenerator roomGeneratorPrefab;
    public int numberOfRooms = 6;
    public int minRoomSize = 3;
    public int maxRoomSize = 6;

    [Header("Generation Logic")]
    [Range(0f, 1f)] public float layerSpawnChance = 0.5f; // Chance to spawn extra rooms in a spiral layer

    [Header("Rotation Logic")]
    // Since our logic assumes the door is at y=1 on a height-2 room, that is the Top side
    public DoorSide prefabDefaultDoorSide = DoorSide.Top;
    [Range(0, 270)] public int globalRotationFix = 0; // Use this if everything is facing the wrong way

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

    // The main grid where 0 is empty, 1 is room, 2 is hallway, 3 is a door connection
    private int[,] grid;
    private List<Vector2Int> roomConnectionPoints = new List<Vector2Int>();
    private List<Vector2Int> debugPath = new List<Vector2Int>();
    private Vector2Int startRoomCenter;

    // A helper struct to keep our door math organized
    struct DoorInfo
    {
        public Vector2Int gridPos;   // The tile inside the room (the door itself)
        public Vector2Int stepPos;   // The tile outside the room (where the hallway connects)
        public Vector2Int dir;       // Which way points out of the room
    }

    private bool even;

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        // Initialize the empty grid and clear old lists
        grid = new int[gridSize, gridSize];
        roomConnectionPoints.Clear();
        debugPath.Clear();

        // Delete any old dungeon objects so we have a clean slate
        if (dungeonContainer != null)
            foreach (Transform child in dungeonContainer) Destroy(child.gameObject);

        // Step 1 is placing the fixed start room
        PlaceStartRoom();

        // Step 2 is spiraling out to place random rooms
        PlaceRoomsSpiralLayers();

        // Step 3 ensures we connect to the closest room first to avoid long weird hallways
        SortConnectionPointsByDistance();

        // Step 4 actually calculates the paths
        ConnectRoomsWithAStar();

        // Step 5 spawns the hallway prefabs based on the grid data
        SpawnWorld();
    }

    void PlaceStartRoom()
    {
        // Calculate the center of the grid to place our first room
        int x = gridSize / 2 - (startRoomWidth / 2);
        int y = gridSize / 2 - (startRoomHeight / 2);
        startRoomCenter = new Vector2Int(x + startRoomWidth / 2, y + startRoomHeight / 2);

        // We use rotation 180 here to ensure the door faces the right way for the start room
        DoorInfo door = GetRotatedDoorInfo(x, y, startRoomWidth, startRoomHeight, 180, startRoomDoorOffset);

        // Safety check to make sure we didn't try to spawn outside the map
        if (!IsInsideGrid(door.stepPos)) return;

        // Mark the grid so hallways know not to walk through this room
        MarkPadding(x, y, startRoomWidth, startRoomHeight);
        MarkRoom(x, y, startRoomWidth, startRoomHeight);

        // Set the connection point
        grid[door.stepPos.x, door.stepPos.y] = 3;
        roomConnectionPoints.Add(door.stepPos);
        ClearEntryPoint(door.stepPos, door.dir);

        // Calculate visual position for the prefab
        float cx = ((startRoomWidth - 1) * tileSize) / 2f;
        float cy = ((startRoomHeight - 1) * tileSize) / 2f;
        Vector3 finalPos = new Vector3((x * tileSize) + cx, 0, (y * tileSize) + cy);

        // Instantiate the room and move the player there
        Instantiate(startRoomPrefab, finalPos, Quaternion.identity, dungeonContainer);
        if (player != null)
        {
            // Teleport the Transform and reset physics so the player doesn't carry momentum
            player.position = finalPos + new Vector3(0, 2, 0);

            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.velocity = Vector3.zero;
                playerRb.position = player.position;
            }

            player.rotation = Quaternion.identity;
        }
    }

    void PlaceRoomsSpiralLayers()
    {
        int roomsPlaced = 0;
        int maxRoomsToAdd = numberOfRooms - 1;

        // Get a list of coordinates spiraling out from the center
        List<Vector2Int> allSpiralPoints = GenerateSpiralPoints();
        Dictionary<int, List<Vector2Int>> layers = new Dictionary<int, List<Vector2Int>>();
        int centerX = gridSize / 2;
        int centerY = gridSize / 2;

        // Group these points into layers or rings around the center
        foreach (Vector2Int p in allSpiralPoints)
        {
            int dist = Mathf.Max(Mathf.Abs(p.x - centerX), Mathf.Abs(p.y - centerY));
            if (!layers.ContainsKey(dist)) layers.Add(dist, new List<Vector2Int>());
            layers[dist].Add(p);
        }

        // Iterate through each layer ring
        foreach (var layerIndex in layers.Keys.OrderBy(k => k))
        {
            if (roomsPlaced >= maxRoomsToAdd) break;
            if (layerIndex == 0) continue;

            // Shuffle the points in this layer so we don't always pick the top-left one
            List<Vector2Int> candidates = layers[layerIndex].OrderBy(x => Random.value).ToList();
            bool placedOneInThisLayer = false;

            foreach (Vector2Int pos in candidates)
            {
                if (roomsPlaced >= maxRoomsToAdd) break;

                // We guarantee at least one room per layer, then rely on chance for more
                bool shouldTry = !placedOneInThisLayer || (Random.value < layerSpawnChance);

                if (shouldTry)
                {
                    // Pick a random size for this specific room
                    int randW = Random.Range(minRoomSize, maxRoomSize + 1);
                    int randH = Random.Range(minRoomSize, maxRoomSize + 1);

                    // Force odd/even adjustments if needed logic was here, simplified for now
                    if (randW % 2 == 0) randW = (Random.Range(0, 2) % 2 == 0) ? randW = randW - 1 : randW = randW + 1;

                    if (TryPlaceRoom(pos.x, pos.y, randW, randH))
                    {
                        roomsPlaced++;
                        placedOneInThisLayer = true;
                    }
                }
            }
        }
    }

    bool TryPlaceRoom(int x, int y, int rawW, int rawH)
    {
        // First we figure out which way this room should face to look at the start room
        float rotation = CalculateRotationTowardsStart(x, y, rawW, rawH);

        // We calculate the local door position dynamically based on the random size we just picked
        // This ensures the door is always centered on the wall
        int doorIndexX = rawW / 2;
        int doorIndexY = 0;

        Vector2Int localDoorPos = new Vector2Int(doorIndexX, doorIndexY);

        // If rotated sideways 90 or 270 degrees, the width and height occupied on the grid swap
        int occupiedW = rawW;
        int occupiedH = rawH;
        bool isSideways = Mathf.Approximately(Mathf.Abs(rotation - 90), 0) || Mathf.Approximately(Mathf.Abs(rotation - 270), 0);
        if (isSideways) { occupiedW = rawH; occupiedH = rawW; }

        // Check if we are out of bounds
        if (x < 4 || y < 4 || x > gridSize - occupiedW - 4 || y > gridSize - occupiedH - 4) return false;

        if (IsAreaClear(x, y, occupiedW, occupiedH))
        {
            // Calculate where the connection point lands on the grid
            DoorInfo door = GetRotatedDoorInfo(x, y, rawW, rawH, rotation, localDoorPos);

            if (!IsInsideGrid(door.stepPos)) return false;
            // Make sure we aren't connecting into an existing wall
            if (grid[door.stepPos.x, door.stepPos.y] != 0) return false;

            // Commit the room to the grid data
            MarkPadding(x, y, occupiedW, occupiedH);
            MarkRoom(x, y, occupiedW, occupiedH);

            grid[door.stepPos.x, door.stepPos.y] = 3;
            roomConnectionPoints.Add(door.stepPos);
            ClearEntryPoint(door.stepPos, door.dir);

            // Calculate the visual center position
            float midX = x + (occupiedW - 1) / 2f;
            float midY = y + (occupiedH - 1) / 2f;
            Vector3 finalPos = new Vector3(midX * tileSize, 0, midY * tileSize);

            // Apply visual nudge logic if needed for rotation alignment
            if (isSideways)
            {
                finalPos.x += roomVisualNudgeZ;
                finalPos.z += roomVisualNudgeX;
            }
            else
            {
                finalPos.x += roomVisualNudgeX;
                finalPos.z += roomVisualNudgeZ;
            }

            // Spawn the empty room object
            GameObject roomObj = Instantiate(roomGeneratorPrefab.gameObject, finalPos, Quaternion.Euler(0, rotation, 0), dungeonContainer);

            // Tell the room script to actually build the walls and floors
            roomObj.GetComponent<RoomGenerator>().BuildRoom(rawW, rawH, localDoorPos);

            return true;
        }
        return false;
    }

    // This function handles the discrete math to find where the door is after rotation
    // It prevents floating point errors from messing up the grid alignment
    DoorInfo GetRotatedDoorInfo(int x, int y, int w, int h, float rotation, Vector2Int localDoor)
    {
        int rot = Mathf.RoundToInt(rotation) % 360;
        if (rot < 0) rot += 360;
        Vector2Int internalPos = Vector2Int.zero;
        Vector2Int direction = Vector2Int.zero;

        switch (rot)
        {
            case 0: // Facing North
                internalPos = new Vector2Int(x + (w - 1 - localDoor.x), y + 1);
                direction = new Vector2Int(0, -1);
                break;
            case 90: // Facing East
                internalPos = new Vector2Int(x + 1, y + localDoor.x);
                direction = new Vector2Int(-1, 0);
                break;
            case 180: // Facing South
                internalPos = new Vector2Int(x + localDoor.x, y + (h - 2));
                direction = new Vector2Int(0, 1);
                break;
            case 270: // Facing West
                internalPos = new Vector2Int(x + (h - 2), y + (w - 1 - localDoor.x));
                direction = new Vector2Int(1, 0);
                break;
        }
        DoorInfo info;
        info.gridPos = internalPos;
        info.stepPos = internalPos + direction;
        info.dir = direction;
        return info;
    }

    // Calculates which direction the room needs to face to look at the start room
    float CalculateRotationTowardsStart(int x, int y, int w, int h)
    {
        Vector2 roomCenter = new Vector2(x + w / 2f, y + h / 2f);
        Vector2 startCenter = new Vector2(startRoomCenter.x, startRoomCenter.y);
        Vector2 dir = startCenter - roomCenter;
        float targetAngle = 0;

        // Determine the dominant direction
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (dir.x > 0) targetAngle = 270;
            else targetAngle = 90;
        }
        else
        {
            if (dir.y > 0) targetAngle = 180;
            else targetAngle = 0;
        }

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

    // figures out which way the door points so we can clear the path
    Vector2Int CalculateClearanceDir(float rotation)
    {
        Vector3 defaultDir = Vector3.zero;
        switch (prefabDefaultDoorSide)
        {
            case DoorSide.Top: defaultDir = new Vector3(0, 0, 1); break;
            case DoorSide.Bottom: defaultDir = new Vector3(0, 0, -1); break;
            case DoorSide.Right: defaultDir = new Vector3(1, 0, 0); break;
            case DoorSide.Left: defaultDir = new Vector3(-1, 0, 0); break;
        }
        Vector3 rotatedDir = Quaternion.Euler(0, rotation, 0) * defaultDir;
        return new Vector2Int(Mathf.RoundToInt(rotatedDir.x), Mathf.RoundToInt(rotatedDir.z));
    }

    // Standard helper to check grid boundaries
    bool IsInsideGrid(Vector2Int p)
    {
        return p.x > 0 && p.y > 0 && p.x < gridSize - 1 && p.y < gridSize - 1;
    }

    // Standard helper to generate points spiraling out from center
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

    // Marks the padding buffer around rooms so hallways don't hug walls
    void MarkPadding(int x, int y, int w, int h)
    {
        for (int i = -1; i <= w; i++) for (int j = -1; j <= h; j++)
                if (IsInsideGrid(new Vector2Int(x + i, y + j))) grid[x + i, y + j] = 4;
    }

    // Marks the actual room tiles
    void MarkRoom(int x, int y, int w, int h)
    {
        for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) grid[x + i, y + j] = 1;
    }

    // Clears the path immediately in front of the door
    void ClearEntryPoint(Vector2Int doorPos, Vector2Int dir)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector2Int entry = doorPos + (dir * i);
            if (IsInsideGrid(entry) && grid[entry.x, entry.y] == 4) grid[entry.x, entry.y] = 0;
        }
    }

    // Checks if the area is free for a new room
    bool IsAreaClear(int startX, int startY, int w, int h)
    {
        for (int x = startX - 4; x < startX + w + 4; x++) for (int y = startY - 4; y < startY + h + 4; y++)
                if (!IsInsideGrid(new Vector2Int(x, y)) || grid[x, y] != 0) return false;
        return true;
    }

    // Debug gizmos to see connections
    void OnDrawGizmos()
    {
        if (roomConnectionPoints == null) return;
        Gizmos.color = Color.red;
        foreach (var p in roomConnectionPoints) Gizmos.DrawSphere(new Vector3(p.x * tileSize, 2, p.y * tileSize), 2f);
    }

    // Reorders the room list so we connect to nearest neighbors
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

    // Main loop to create A* paths between sorted rooms
    void ConnectRoomsWithAStar()
    {
        for (int i = 0; i < roomConnectionPoints.Count - 1; i++)
        {
            List<Vector2Int> path = FindPath(roomConnectionPoints[i], roomConnectionPoints[i + 1]);
            if (path != null) foreach (Vector2Int pos in path) if (grid[pos.x, pos.y] != 3) grid[pos.x, pos.y] = 2;
        }
    }

    // The A* pathfinding implementation
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
                if (!IsInsideGrid(n)) continue;
                // Avoid rooms(1) and padding(4), but target is allowed
                bool blocked = (grid[n.x, n.y] == 1 || grid[n.x, n.y] == 4);
                if (n == target) blocked = false;
                if (!blocked && !cameFrom.ContainsKey(n)) { queue.Enqueue(n); cameFrom[n] = curr; }
            }
        }
        return null;
    }

    // Iterates the grid and spawns the correct prefab for hallways
    void SpawnWorld()
    {
        for (int x = 0; x < gridSize; x++) for (int y = 0; y < gridSize; y++)
                if (grid[x, y] == 2 || grid[x, y] == 3) SpawnTile(x, y);
    }

    // Determines if a tile is a straight hall, corner, or intersection
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

    // Helper check for spawning logic
    bool IsPath(int x, int y)
    {
        if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) return false;
        return grid[x, y] == 2 || grid[x, y] == 3;
    }
}