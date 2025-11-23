using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridSize = 25;           // Size of the map (50x50 cells)
    public float tileSize = 9.5f;         // DISTANCE between two tiles (Important!)

    [Header("Room Settings")]
    public int numberOfRooms = 2;
    public int roomWidthInTiles = 4;    // How wide is your Large Room? (e.g. 3 tiles)
    public int roomHeightInTiles = 4;   // How long is your Large Room?

    [Header("Your Prefabs")]
    // Drag your "Combined" prefabs here (Floor + Walls attached)
    public GameObject straightHallway;
    public GameObject cornerHallway;
    public GameObject entrancePiece;    // The piece with the door frame
    public GameObject tIntersection;    // A floor piece with NO walls (for 3-way splits)
    public GameObject largeRoom;        // Your pre-built room

    [Header("Container")]
    public Transform dungeonContainer;  // Drag an empty GameObject here to keep hierarchy clean

    // 0 = Empty, 1 = Room, 2 = Hallway
    private int[,] grid;
    private List<Vector2Int> roomCenters = new List<Vector2Int>();

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        // 1. Setup Grid
        grid = new int[gridSize, gridSize];
        roomCenters.Clear();

        // Clear old dungeon
        if (dungeonContainer != null)
        {
            foreach (Transform child in dungeonContainer) Destroy(child.gameObject);
        }

        // 2. Place the Large Rooms first
        PlaceRooms();

        // 3. Connect the rooms with "virtual" hallways
        ConnectRooms();

        // 4. Spawn the actual 3D objects based on the grid data
        SpawnWorld();
    }

    void PlaceRooms()
    {
        int roomsPlaced = 0;
        int attempts = 0;

        // Try to place rooms
        while (roomsPlaced < numberOfRooms && attempts < 100)
        {
            attempts++;

            // Pick a random grid coordinate (bottom-left corner of the room)
            int x = Random.Range(2, gridSize - roomWidthInTiles - 2);
            int y = Random.Range(2, gridSize - roomHeightInTiles - 2);

            // INCREASED BUFFER: We use '2' here instead of '1' to keep rooms further apart
            // This prevents the "Blob" look where rooms touch each other.
            if (IsAreaClear(x, y, roomWidthInTiles, roomHeightInTiles))
            {
                // 1. Mark grid as ROOM (1)
                for (int i = 0; i < roomWidthInTiles; i++)
                {
                    for (int j = 0; j < roomHeightInTiles; j++)
                    {
                        grid[x + i, y + j] = 1;
                    }
                }

                // 2. Add the center for hallway connections
                // We use integer division here, so it picks a specific tile to connect to.
                roomCenters.Add(new Vector2Int(x + roomWidthInTiles / 2, y + roomHeightInTiles / 2));

                // 3. SPAWN MATH FIX
                // We calculate the exact center position in World Space.
                // Formula: StartPos + (Half the total width) - (Half a tile)
                // Or simplified: (Coordinate * Size) + (Size * (Width-1) / 2)

                float centerOffsetX = ((roomWidthInTiles - 1) * tileSize) / 2f;
                float centerOffsetY = ((roomHeightInTiles - 1) * tileSize) / 2f;

                Vector3 finalPos = new Vector3(
                    (x * tileSize) + centerOffsetX,
                    0,
                    (y * tileSize) + centerOffsetY
                );

                Instantiate(largeRoom, finalPos, Quaternion.identity, dungeonContainer);

                roomsPlaced++;
            }
        }
    }

    bool IsAreaClear(int startX, int startY, int w, int h)
    {
        // Check area + 2 tile buffer so rooms don't fuse together
        for (int x = startX - 2; x < startX + w + 2; x++)
        {
            for (int y = startY - 2; y < startY + h + 2; y++)
            {
                // Safety check to stay inside array bounds
                if (x >= 0 && x < gridSize && y >= 0 && y < gridSize)
                {
                    if (grid[x, y] != 0) return false;
                }
            }
        }
        return true;
    }

    void ConnectRooms()
    {
        // Connect Room A to Room B, Room B to Room C, etc.
        for (int i = 0; i < roomCenters.Count - 1; i++)
        {
            Vector2Int start = roomCenters[i];
            Vector2Int end = roomCenters[i + 1];

            // Create an L-shaped path
            Vector2Int current = start;

            // Move Horizontally
            while (current.x != end.x)
            {
                if (current.x < end.x) current.x++;
                else current.x--;

                // If it's not a room, mark it as hallway
                if (grid[current.x, current.y] == 0) grid[current.x, current.y] = 2;
            }

            // Move Vertically
            while (current.y != end.y)
            {
                if (current.y < end.y) current.y++;
                else current.y--;

                if (grid[current.x, current.y] == 0) grid[current.x, current.y] = 2;
            }
        }
    }

    void SpawnWorld()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                // Only spawn if it is a Hallway (2)
                // We skip Rooms (1) because we already spawned the large prefabs in PlaceRooms()
                if (grid[x, y] == 2)
                {
                    SpawnTile(x, y);
                }
            }
        }
    }

    void SpawnTile(int x, int y)
    {
        // 1. Check Neighbors
        bool n = grid[x, y + 1] != 0;
        bool s = grid[x, y - 1] != 0;
        bool e = grid[x + 1, y] != 0;
        bool w = grid[x - 1, y] != 0;

        // 2. Check for Rooms
        bool nRoom = grid[x, y + 1] == 1;
        bool sRoom = grid[x, y - 1] == 1;
        bool eRoom = grid[x + 1, y] == 1;
        bool wRoom = grid[x - 1, y] == 1;

        GameObject prefab = null;
        float rotation = 0;
        int neighborCount = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);

        // A. Intersections
        if (neighborCount >= 3)
        {
            prefab = tIntersection;
        }
        // B. Entrances
        else if (nRoom || sRoom || eRoom || wRoom)
        {
            prefab = entrancePiece;
            if (nRoom) rotation = 0;
            if (sRoom) rotation = 180;
            if (eRoom) rotation = 90;
            if (wRoom) rotation = 270;
        }
        // C. Straight Hallways
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
        // D. Corners (Assumes Default is South+East)
        else
        {
            prefab = cornerHallway;
            if (s && e) rotation = 0;
            if (s && w) rotation = 90;
            if (n && w) rotation = 180;
            if (n && e) rotation = 270;
        }

        if (prefab != null)
        {
            // Simple center-to-center spawning
            Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);
            Instantiate(prefab, pos, Quaternion.Euler(0, rotation, 0), dungeonContainer);
        }
    }
}