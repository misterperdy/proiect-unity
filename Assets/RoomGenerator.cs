using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomGenerator : MonoBehaviour
{
    [Header("Settings")]
    public float tileSize = 9.5f;

    [Header("Wall Configuration")]
    public float wallPrefabWidth = 2.0f;
    public float pillarInset = 0.5f;

    [Header("Building Blocks")]
    public GameObject floorTile;
    public GameObject wallStraight;
    public GameObject cornerColumn;

    [Header("Decorations")]
    public GameObject[] wallProps;
    public GameObject[] cornerProps;
    public GameObject[] centerProps;

    [Range(0, 1)] public float decorationDensity = 0.3f;

    public void BuildRoom(int width, int height, Vector2Int doorLocalPos)
    {
        float centeringX = ((width - 1) * tileSize) / 2f;
        float centeringZ = ((height - 1) * tileSize) / 2f;
        Vector3 centeringVector = new Vector3(centeringX, 0, centeringZ);
        float offset = tileSize / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 rawPos = new Vector3(x * tileSize, 0, y * tileSize);
                Vector3 localPos = rawPos - centeringVector;

                // 1. PLACE FLOOR
                Vector3 worldPos = transform.TransformPoint(localPos);
                Instantiate(floorTile, worldPos, transform.rotation, transform);

                // 2. EDGES
                bool isLeft = (x == 0);
                bool isRight = (x == width - 1);
                bool isBottom = (y == 0);
                bool isTop = (y == height - 1);

                if (x == doorLocalPos.x && y == doorLocalPos.y) continue;

                bool placedWall = false;

                if (isBottom)
                {
                    bool startPillar = (x == 0);
                    bool endPillar = (x == width - 1);
                    bool startDoor = (x - 1 == doorLocalPos.x && y == doorLocalPos.y);
                    bool endDoor = (x + 1 == doorLocalPos.x && y == doorLocalPos.y);
                    SpawnWallRow(localPos + new Vector3(0, 0, -offset), 0, startPillar, endPillar, startDoor, endDoor);
                    placedWall = true;
                }
                if (isTop)
                {
                    bool startPillar = (x == width - 1);
                    bool endPillar = (x == 0);
                    bool startDoor = (x + 1 == doorLocalPos.x && y == doorLocalPos.y);
                    bool endDoor = (x - 1 == doorLocalPos.x && y == doorLocalPos.y);
                    SpawnWallRow(localPos + new Vector3(0, 0, offset), 180, startPillar, endPillar, startDoor, endDoor);
                    placedWall = true;
                }
                if (isLeft)
                {
                    bool startPillar = (y == height - 1);
                    bool endPillar = (y == 0);
                    bool startDoor = (x == doorLocalPos.x && y + 1 == doorLocalPos.y);
                    bool endDoor = (x == doorLocalPos.x && y - 1 == doorLocalPos.y);
                    SpawnWallRow(localPos + new Vector3(-offset, 0, 0), 90, startPillar, endPillar, startDoor, endDoor);
                    placedWall = true;
                }
                if (isRight)
                {
                    bool startPillar = (y == 0);
                    bool endPillar = (y == height - 1);
                    bool startDoor = (x == doorLocalPos.x && y - 1 == doorLocalPos.y);
                    bool endDoor = (x == doorLocalPos.x && y + 1 == doorLocalPos.y);
                    SpawnWallRow(localPos + new Vector3(offset, 0, 0), 270, startPillar, endPillar, startDoor, endDoor);
                    placedWall = true;
                }

                if (!placedWall) PlaceDecorations(localPos);
            }
        }

        // 3. PILLARS
        float roomMinX = (-offset) - centeringX;
        float roomMaxX = ((width - 1) * tileSize + offset) - centeringX;
        float roomMinZ = (-offset) - centeringZ;
        float roomMaxZ = ((height - 1) * tileSize + offset) - centeringZ;

        SpawnProp(cornerColumn, new Vector3(roomMinX, 0.268489f, roomMinZ), 0);
        SpawnProp(cornerColumn, new Vector3(roomMaxX, 0.268489f, roomMinZ), 0);
        SpawnProp(cornerColumn, new Vector3(roomMinX, 0.268489f, roomMaxZ), 0);
        SpawnProp(cornerColumn, new Vector3(roomMaxX, 0.268489f, roomMaxZ), 0);

        CleanupDuplicateWalls();
        CleanupDuplicateWalls();
    }

    void SpawnWallRow(Vector3 edgeCenter, float rotY, bool startIsPillar, bool endIsPillar, bool startIsDoor, bool endIsDoor)
    {
        if (wallStraight == null) return;

        Quaternion rotation = Quaternion.Euler(0, rotY, 0);
        Vector3 rightDir = rotation * Vector3.right;
        Vector3 leftDir = rotation * Vector3.left;

        // Boundaries
        Vector3 rawStartPos = edgeCenter + (leftDir * (tileSize / 2f));
        Vector3 rawEndPos = edgeCenter + (rightDir * (tileSize / 2f));

        // Start Buffer
        float startBuffer = startIsPillar ? pillarInset : 0f;
        // End Buffer (Limits)
        float endLimit = tileSize;
        if (endIsPillar) endLimit -= pillarInset;
        // if endIsDoor, endLimit stays tileSize (0 buffer)

        Vector3 buildStartPos = rawStartPos + (rightDir * (startBuffer + wallPrefabWidth / 2f));
        int wallCount = Mathf.CeilToInt(tileSize / wallPrefabWidth);

        bool gapExists = false;

        for (int i = 0; i < wallCount; i++)
        {
            float myPos = i * wallPrefabWidth;

            // Check Collision with END Limit (Pillar OR Door)
            if ((endIsPillar || endIsDoor) && (myPos + wallPrefabWidth > endLimit - 0.01f))
            {
                gapExists = true; // We stopped early, we need to fill the gap
                break;
            }

            Vector3 height = new Vector3(0f, 0.268489f, 0);
            Vector3 currentPos = buildStartPos + height + (rightDir * myPos);

            Vector3 worldPos = transform.TransformPoint(currentPos);
            Quaternion worldRot = transform.rotation * rotation;
            Instantiate(wallStraight, worldPos, worldRot, transform);

            if (Random.value > 0.8f)
            {
          
                Vector3 baseHeight = new Vector3(0, 3, 0);
                Vector3 pushOutOffset = new Vector3(0, 0, 0.3f);

                Vector3 rotatedOffset = Quaternion.Euler(0, rotY, 0) * pushOutOffset;

                Vector3 finalTorchPos = currentPos + baseHeight + rotatedOffset;

                SpawnProp(GetRandom(wallProps), finalTorchPos, rotY);
            }
        }

        // If we stopped early due to a boundary (Pillar or Door), fill the gap from the back.
        if (gapExists)
        {
            float fillBuffer = 0f;
            if (endIsPillar) fillBuffer = pillarInset;
            // if endIsDoor, fillBuffer remains 0 (Align perfectly to edge)

            // Calculate exact end position to back up from
            Vector3 height = new Vector3(0f, 0.268489f, 0);
            Vector3 fillPos = rawEndPos + height - (rightDir * (fillBuffer + wallPrefabWidth / 2f));

            Vector3 worldPos = transform.TransformPoint(fillPos);
            Quaternion worldRot = transform.rotation * rotation;
            Instantiate(wallStraight, worldPos, worldRot, transform);
        }
    }

    void PlaceDecorations(Vector3 localPos)
    {
        if (Random.value > decorationDensity) return;
        Vector3 height = new Vector3 (0, 0.2525f, 0);
        Vector3 randomXZ = new Vector3(Random.Range(0f, 4f), 0, Random.Range(0f, 4f));
        localPos = localPos + height + randomXZ;
        SpawnProp(GetRandom(centerProps), localPos , Random.Range(0, 4) * 90);
    }

    void SpawnProp(GameObject prefab, Vector3 localPos, float rotY)
    {
        if (prefab == null) return;
        Vector3 worldPos = transform.TransformPoint(localPos);
        Quaternion worldRot = transform.rotation * Quaternion.Euler(0, rotY, 0);
        Instantiate(prefab, worldPos, worldRot, transform);
    }

    GameObject GetRandom(GameObject[] list)
    {
        if (list.Length == 0) return null;
        return list[Random.Range(0, list.Length)];
    }

    void CleanupDuplicateWalls()
    {
        // 1. Get all walls created inside this room
        // We assume walls are direct children and have the name of the prefab
        List<Transform> walls = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.Contains(wallStraight.name))
            {
                walls.Add(child);
            }
        }

        // 2. Iterate and compare
        // We traverse backwards so we can destroy objects without breaking the loop
        for (int i = walls.Count - 1; i >= 0; i--)
        {
            if (walls[i] == null) continue; // Already destroyed

            for (int j = i - 1; j >= 0; j--)
            {
                if (walls[j] == null) continue;

                Transform wallA = walls[i];
                Transform wallB = walls[j];

                // CHECK 1: Are they essentially in the same position?
                // We use a small threshold (e.g., 0.1 units)
                float dist = Vector3.Distance(wallA.position, wallB.position);

                // CHECK 2: Are they facing the same way? (Dot Product > 0.9)
                // This prevents deleting a corner wall that just happens to be close
                float angle = Quaternion.Dot(wallA.rotation, wallB.rotation);

                if (dist < 0.1f && Mathf.Abs(angle) > 0.9f)
                {
                    // They are duplicates!
                    // Destroy the "Gap Filler" (usually the later one in the list)
                    DestroyImmediate(wallA.gameObject);
                    break; // Stop checking this wall, it's gone
                }
            }
        }
    }
}