using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridSize = 25;
    public float tileSize = 10f;

    [Header("Room Settings")]
    public int numberOfRooms = 8;
    public int roomWidthInTiles = 2;
    public int roomHeightInTiles = 2;
    public float roomVisualNudgeX = 0f;
    public float roomVisualNudgeZ = 0f;

    [Header("Door Logic")]
    public Vector2Int doorOffset = new Vector2Int(1, -1);

    [Header("Prefabs")]
    public GameObject straightHallway;
    public GameObject cornerHallway;
    public GameObject tJunction;      
    public GameObject crossIntersection; 
    public GameObject largeRoom;

    [Header("Container")]
    public Transform dungeonContainer;

    private int[,] grid;
    private List<Vector2Int> roomConnectionPoints = new List<Vector2Int>();

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        grid = new int[gridSize, gridSize];
        roomConnectionPoints.Clear();

        if (dungeonContainer != null)
            foreach (Transform child in dungeonContainer) Destroy(child.gameObject);

        PlaceRooms();
        ConnectRoomsWithAStar();
        SpawnWorld();
    }

    void PlaceRooms()
    {
        int roomsPlaced = 0;
        int attempts = 0;

        while (roomsPlaced < numberOfRooms && attempts < 100)
        {
            attempts++;
            int x = Random.Range(3, gridSize - roomWidthInTiles - 3);
            int y = Random.Range(3, gridSize - roomHeightInTiles - 3);

            if (IsAreaClear(x, y, roomWidthInTiles, roomHeightInTiles))
            {
                // 1. PADDING
                for (int i = -1; i <= roomWidthInTiles; i++)
                {
                    for (int j = -1; j <= roomHeightInTiles; j++)
                    {
                        if (x + i >= 0 && x + i < gridSize && y + j >= 0 && y + j < gridSize)
                            grid[x + i, y + j] = 4;
                    }
                }

                // 2. ROOM
                for (int i = 0; i < roomWidthInTiles; i++)
                {
                    for (int j = 0; j < roomHeightInTiles; j++)
                    {
                        grid[x + i, y + j] = 1;
                    }
                }

                // 3. DOOR STEP
                Vector2Int doorPos = new Vector2Int(x + doorOffset.x, y + doorOffset.y);
                grid[doorPos.x, doorPos.y] = 3;
                roomConnectionPoints.Add(doorPos);

                // 4. CLEAR ENTRY
                Vector2Int entryPoint = doorPos;
                if (doorOffset.y < 0) entryPoint.y -= 1;
                else if (doorOffset.y > 0) entryPoint.y += 1;
                else if (doorOffset.x < 0) entryPoint.x -= 1;
                else if (doorOffset.x > 0) entryPoint.x += 1;

                if (grid[entryPoint.x, entryPoint.y] == 4)
                    grid[entryPoint.x, entryPoint.y] = 0;

                // 5. VISUALS
                float centerOffsetX = ((roomWidthInTiles - 1) * tileSize) / 2f;
                float centerOffsetY = ((roomHeightInTiles - 1) * tileSize) / 2f;
                Vector3 finalPos = new Vector3(
                    (x * tileSize) + centerOffsetX + roomVisualNudgeX,
                    0,
                    (y * tileSize) + centerOffsetY + roomVisualNudgeZ
                );
                Instantiate(largeRoom, finalPos, Quaternion.identity, dungeonContainer);
                roomsPlaced++;
            }
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
                        grid[pos.x, pos.y] = 2;
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

        while (queue.Count > 0 && safetyBreak < 5000)
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
                if (grid[x, y] == 2 || grid[x, y] == 3)
                {
                    SpawnTile(x, y);
                }
            }
        }
    }

    void SpawnTile(int x, int y)
    {
        bool n = IsPath(x, y + 1);
        bool s = IsPath(x, y - 1);
        bool e = IsPath(x + 1, y);
        bool w = IsPath(x - 1, y);

        if (grid[x, y] == 3) // DoorStep special handling
        {
            if (grid[x, y + 1] == 1) n = true;
            if (grid[x, y - 1] == 1) s = true;
            if (grid[x + 1, y] == 1) e = true;
            if (grid[x - 1, y] == 1) w = true;
        }

        GameObject prefab = null;
        float rotation = 0;
        int count = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);

        
        if (count == 4)
        {
            // 4-Way Intersection: Needs 0 walls
            prefab = crossIntersection;
        }
        else if (count == 3)
        {
            // 3-Way T-Junction: Needs 1 wall
            prefab = tJunction;

            // Logic: Rotate the wall to face the "missing" neighbor
            // Assumes your prefab has the wall on the NORTH side by default
            if (!n) rotation = 0;     // Missing North -> Wall North
            else if (!e) rotation = 90; // Missing East -> Wall East
            else if (!s) rotation = 180;// Missing South -> Wall South
            else if (!w) rotation = 270;// Missing West -> Wall West
        }
        else if (n && s)
        {
            prefab = straightHallway;
            rotation = 0;
        }
        else if (e && w)
        {
            prefab = straightHallway;
            rotation = 90;
        }
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