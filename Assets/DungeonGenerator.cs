using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridSize = 60;
    public float tileSize = 10f;

    [Header("Player")]
    public Transform player;

    [Header("Start Room")]
    public GameObject startRoomPrefab;
    public int startRoomWidth = 5;
    public int startRoomHeight = 2; // Updated to 2 per your request
    // Your specific offset: X=2 (Middle), Y=1 (Top Row/Edge)
    public Vector2Int startRoomDoorOffset = new Vector2Int(2, 1);

    [Header("Standard Rooms")]
    public GameObject largeRoomPrefab;
    public int numberOfRooms = 6;
    public int roomWidthInTiles = 4;
    public int roomHeightInTiles = 2; // Updated to 2 per your request
    public float roomVisualNudgeX = 5.0f;
    public float roomVisualNudgeZ = 0f;
    // Your specific offset: X=1, Y=1
    public Vector2Int standardDoorOffset = new Vector2Int(1, 1);

    [Header("Prefabs")]
    public GameObject straightHallway;
    public GameObject cornerHallway;
    public GameObject tJunction;
    public GameObject crossIntersection;

    [Header("Container")]
    public Transform dungeonContainer;

    // 0=Void, 1=Room, 2=Hallway, 3=DoorStep, 4=Padding
    private int[,] grid;
    private List<Vector2Int> roomConnectionPoints = new List<Vector2Int>();

    // Debug Lists
    private List<Vector2Int> debugPath = new List<Vector2Int>();

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

        // 1. Place Rooms
        PlaceStartRoom();
        PlaceRandomRooms();

        // 2. Sort to find nearest neighbors
        SortConnectionPointsByDistance();

        // 3. Connect
        ConnectRoomsWithAStar();
        SpawnWorld();
    }

    void OnDrawGizmos()
    {
        if (roomConnectionPoints == null) return;
        Gizmos.color = Color.red;
        foreach (var p in roomConnectionPoints)
        {
            Vector3 pos = new Vector3(p.x * tileSize, 2, p.y * tileSize);
            Gizmos.DrawSphere(pos, 2f);
        }
    }

    void SortConnectionPointsByDistance()
    {
        if (roomConnectionPoints.Count < 2) return;
        List<Vector2Int> sorted = new List<Vector2Int>();
        List<Vector2Int> pool = new List<Vector2Int>(roomConnectionPoints);

        Vector2Int current = pool[0]; // Start Room
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
                if (dst < minDst)
                {
                    minDst = dst;
                    closest = pool[i];
                    closestIndex = i;
                }
            }
            sorted.Add(closest);
            current = closest;
            pool.RemoveAt(closestIndex);
        }
        roomConnectionPoints = sorted;
    }

    void PlaceStartRoom()
    {
        // --- RANDOM START POSITION ---
        // We define a range that keeps the room away from the very edge of the map
        int x = Random.Range(5, gridSize - startRoomWidth - 5);
        int y = Random.Range(5, gridSize - startRoomHeight - 5);

        MarkPadding(x, y, startRoomWidth, startRoomHeight);
        MarkRoom(x, y, startRoomWidth, startRoomHeight);

        Vector2Int doorPos = new Vector2Int(x + startRoomDoorOffset.x, y + startRoomDoorOffset.y);
        grid[doorPos.x, doorPos.y] = 3;
        roomConnectionPoints.Add(doorPos);

        ClearEntryPoint(doorPos, startRoomDoorOffset);

        float centerOffsetX = ((startRoomWidth - 1) * tileSize) / 2f;
        float centerOffsetY = ((startRoomHeight - 1) * tileSize) / 2f;
        Vector3 finalPos = new Vector3((x * tileSize) + centerOffsetX, 0, (y * tileSize) + centerOffsetY);

        Instantiate(startRoomPrefab, finalPos, Quaternion.identity, dungeonContainer);

        if (player != null) player.position = finalPos + new Vector3(0, 1, 0);
    }

    void PlaceRandomRooms()
    {
        int roomsPlaced = 0;
        int attempts = 0;

        while (roomsPlaced < numberOfRooms - 1 && attempts < 20000)
        {
            attempts++;
            int x = Random.Range(4, gridSize - roomWidthInTiles - 4);
            int y = Random.Range(4, gridSize - roomHeightInTiles - 4);

            if (IsAreaClear(x, y, roomWidthInTiles, roomHeightInTiles))
            {
                MarkPadding(x, y, roomWidthInTiles, roomHeightInTiles);
                MarkRoom(x, y, roomWidthInTiles, roomHeightInTiles);

                Vector2Int doorPos = new Vector2Int(x + standardDoorOffset.x, y + standardDoorOffset.y);
                grid[doorPos.x, doorPos.y] = 3;
                roomConnectionPoints.Add(doorPos);

                ClearEntryPoint(doorPos, standardDoorOffset);

                float centerOffsetX = ((roomWidthInTiles - 1) * tileSize) / 2f;
                float centerOffsetY = ((roomHeightInTiles - 1) * tileSize) / 2f;
                Vector3 finalPos = new Vector3(
                    (x * tileSize) + centerOffsetX + roomVisualNudgeX,
                    0,
                    (y * tileSize) + centerOffsetY + roomVisualNudgeZ
                );

                Instantiate(largeRoomPrefab, finalPos, Quaternion.identity, dungeonContainer);
                roomsPlaced++;
            }
        }
    }

    void MarkPadding(int x, int y, int w, int h)
    {
        for (int i = -1; i <= w; i++)
        {
            for (int j = -1; j <= h; j++)
            {
                if (x + i >= 0 && x + i < gridSize && y + j >= 0 && y + j < gridSize)
                    grid[x + i, y + j] = 4;
            }
        }
    }

    void MarkRoom(int x, int y, int w, int h)
    {
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                grid[x + i, y + j] = 1;
            }
        }
    }

    void ClearEntryPoint(Vector2Int doorPos, Vector2Int offset)
    {
        Vector2Int entry = doorPos;
        // Since Y=1 usually means "Up/Top", we clear the tile ABOVE.
        if (offset.y > 0) entry.y += 1;
        else if (offset.y < 0) entry.y -= 1;
        else if (offset.x > 0) entry.x += 1;
        else if (offset.x < 0) entry.x -= 1;

        if (entry.x >= 0 && entry.x < gridSize && entry.y >= 0 && entry.y < gridSize)
        {
            if (grid[entry.x, entry.y] == 4) grid[entry.x, entry.y] = 0;
        }
    }

    bool IsAreaClear(int startX, int startY, int w, int h)
    {
        for (int x = startX - 4; x < startX + w + 4; x++)
        {
            for (int y = startY - 4; y < startY + h + 4; y++)
            {
                if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) return false;
                if (grid[x, y] != 0) return false;
            }
        }
        return true;
    }

    void ConnectRoomsWithAStar()
    {
        for (int i = 0; i < roomConnectionPoints.Count - 1; i++)
        {
            Vector2Int start = roomConnectionPoints[i];
            Vector2Int end = roomConnectionPoints[i + 1];
            List<Vector2Int> path = FindPath(start, end);
            if (path != null)
            {
                foreach (Vector2Int pos in path)
                {
                    if (grid[pos.x, pos.y] != 3)
                    {
                        grid[pos.x, pos.y] = 2;
                        debugPath.Add(pos);
                    }
                }
            }
        }
    }

    List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        cameFrom[start] = start;
        bool found = false;
        int safetyBreak = 0;

        while (queue.Count > 0 && safetyBreak < 10000)
        {
            safetyBreak++;
            Vector2Int current = queue.Dequeue();
            if (current == target) { found = true; break; }

            Vector2Int[] neighbors = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };

            foreach (Vector2Int dir in neighbors)
            {
                Vector2Int next = current + dir;
                if (next.x < 1 || next.y < 1 || next.x >= gridSize - 1 || next.y >= gridSize - 1) continue;

                int tileType = grid[next.x, next.y];
                bool isBlocked = (tileType == 1 || tileType == 4);
                if (next == target) isBlocked = false;

                if (!isBlocked && !cameFrom.ContainsKey(next))
                {
                    queue.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }
        if (found)
        {
            Vector2Int curr = target;
            while (curr != start) { path.Add(curr); curr = cameFrom[curr]; }
            path.Reverse();
        }
        return path;
    }

    void SpawnWorld()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y] == 2 || grid[x, y] == 3) SpawnTile(x, y);
            }
        }
    }

    void SpawnTile(int x, int y)
    {
        if (grid[x, y] == 3) return; // Leave door OPEN

        bool n = IsPath(x, y + 1);
        bool s = IsPath(x, y - 1);
        bool e = IsPath(x + 1, y);
        bool w = IsPath(x - 1, y);

        GameObject prefab = null;
        float rotation = 0;
        int count = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);

        if (count >= 3)
        {
            prefab = (count == 4) ? crossIntersection : tJunction;
            if (count == 3)
            {
                if (!n) rotation = 0;
                else if (!e) rotation = 90;
                else if (!s) rotation = 180;
                else if (!w) rotation = 270;
            }
        }
        else if (n && s) { prefab = straightHallway; rotation = 0; }
        else if (e && w) { prefab = straightHallway; rotation = 90; }
        else
        {
            prefab = cornerHallway;
            if (s && e) rotation = 0;
            if (s && w) rotation = 90;
            if (n && w) rotation = 180;
            if (n && e) rotation = 270;

            if (count == 1)
            {
                prefab = straightHallway;
                if (n || s) rotation = 0; else rotation = 90;
            }
        }

        if (prefab != null)
        {
            Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);
            Instantiate(prefab, pos, Quaternion.Euler(0, rotation, 0), dungeonContainer);
        }
    }

    bool IsPath(int x, int y)
    {
        if (x < 0 || y < 0 || x >= gridSize || y >= gridSize) return false;
        return grid[x, y] == 2 || grid[x, y] == 3;
    }
}