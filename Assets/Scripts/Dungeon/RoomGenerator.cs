using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public void BuildRoom(int width, int height, Vector2Int doorLocalPos, Material floorMat, Material wallMat)
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

                // 1. Place Floor
                Vector3 worldPos = transform.TransformPoint(localPos);
                GameObject floor = Instantiate(floorTile, worldPos, transform.rotation, transform);
                ApplyMaterial(floor, floorMat);

                // 2. Determine Walls
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
                    SpawnWallRow(localPos + new Vector3(0, 0, -offset), 0, startPillar, endPillar, startDoor, endDoor, wallMat);
                    placedWall = true;
                }
                if (isTop)
                {
                    bool startPillar = (x == width - 1);
                    bool endPillar = (x == 0);
                    bool startDoor = (x + 1 == doorLocalPos.x && y == doorLocalPos.y);
                    bool endDoor = (x - 1 == doorLocalPos.x && y == doorLocalPos.y);
                    SpawnWallRow(localPos + new Vector3(0, 0, offset), 180, startPillar, endPillar, startDoor, endDoor, wallMat);
                    placedWall = true;
                }
                if (isLeft)
                {
                    bool startPillar = (y == height - 1);
                    bool endPillar = (y == 0);
                    bool startDoor = (x == doorLocalPos.x && y + 1 == doorLocalPos.y);
                    bool endDoor = (x == doorLocalPos.x && y - 1 == doorLocalPos.y);
                    SpawnWallRow(localPos + new Vector3(-offset, 0, 0), 90, startPillar, endPillar, startDoor, endDoor, wallMat);
                    placedWall = true;
                }
                if (isRight)
                {
                    bool startPillar = (y == 0);
                    bool endPillar = (y == height - 1);
                    bool startDoor = (x == doorLocalPos.x && y - 1 == doorLocalPos.y);
                    bool endDoor = (x == doorLocalPos.x && y + 1 == doorLocalPos.y);
                    SpawnWallRow(localPos + new Vector3(offset, 0, 0), 270, startPillar, endPillar, startDoor, endDoor, wallMat);
                    placedWall = true;
                }

                if (!placedWall) PlaceDecorations(localPos);
            }
        }

        // 3. Place Corner Pillars
        float roomMinX = (-offset) - centeringX;
        float roomMaxX = ((width - 1) * tileSize + offset) - centeringX;
        float roomMinZ = (-offset) - centeringZ;
        float roomMaxZ = ((height - 1) * tileSize + offset) - centeringZ;

        // Note: Hardcoded Y pos used in previous versions is maintained here for consistency
        float pillarY = 0.268489f;
        SpawnProp(cornerColumn, new Vector3(roomMinX, pillarY, roomMinZ), 0, wallMat);
        SpawnProp(cornerColumn, new Vector3(roomMaxX, pillarY, roomMinZ), 0, wallMat);
        SpawnProp(cornerColumn, new Vector3(roomMinX, pillarY, roomMaxZ), 0, wallMat);
        SpawnProp(cornerColumn, new Vector3(roomMaxX, pillarY, roomMaxZ), 0, wallMat);

        CleanupDuplicateWalls();
    }

    void SpawnWallRow(Vector3 edgeCenter, float rotY, bool startIsPillar, bool endIsPillar, bool startIsDoor, bool endIsDoor, Material mat)
    {
        if (wallStraight == null) return;

        Quaternion rotation = Quaternion.Euler(0, rotY, 0);
        Vector3 rightDir = rotation * Vector3.right;
        Vector3 leftDir = rotation * Vector3.left;

        Vector3 rawStartPos = edgeCenter + (leftDir * (tileSize / 2f));
        Vector3 rawEndPos = edgeCenter + (rightDir * (tileSize / 2f));

        float startBuffer = startIsPillar ? pillarInset : 0f;
        float endLimit = tileSize;
        if (endIsPillar) endLimit -= pillarInset;

        Vector3 buildStartPos = rawStartPos + (rightDir * (startBuffer + wallPrefabWidth / 2f));
        int wallCount = Mathf.CeilToInt(tileSize / wallPrefabWidth);

        bool gapExists = false;

        for (int i = 0; i < wallCount; i++)
        {
            float myPos = i * wallPrefabWidth;

            // Prevent walls from clipping into end pillars or doors
            if ((endIsPillar || endIsDoor) && (myPos + wallPrefabWidth > endLimit - 0.01f))
            {
                gapExists = true;
                break;
            }

            Vector3 height = new Vector3(0f, 0.268489f, 0);
            Vector3 currentPos = buildStartPos + height + (rightDir * myPos);

            Vector3 worldPos = transform.TransformPoint(currentPos);
            Quaternion worldRot = transform.rotation * rotation;

            GameObject w = Instantiate(wallStraight, worldPos, worldRot, transform);
            ApplyMaterial(w, mat);

            // Chance to spawn wall props (torches)
            if (Random.value > 0.8f)
            {
                Vector3 baseHeight = new Vector3(0, 3, 0);
                Vector3 pushOutOffset = new Vector3(0, 0, 0.3f);
                Vector3 rotatedOffset = Quaternion.Euler(0, rotY, 0) * pushOutOffset;
                Vector3 finalTorchPos = currentPos + baseHeight + rotatedOffset;

                SpawnProp(GetRandom(wallProps), finalTorchPos, rotY, null);
            }
        }

        // Fill gaps near pillars with one final wall segment
        if (gapExists)
        {
            float fillBuffer = 0f;
            if (endIsPillar) fillBuffer = pillarInset;

            Vector3 height = new Vector3(0f, 0.268489f, 0);
            Vector3 fillPos = rawEndPos + height - (rightDir * (fillBuffer + wallPrefabWidth / 2f));

            Vector3 worldPos = transform.TransformPoint(fillPos);
            Quaternion worldRot = transform.rotation * rotation;

            GameObject w = Instantiate(wallStraight, worldPos, worldRot, transform);
            ApplyMaterial(w, mat);
        }
    }

    void PlaceDecorations(Vector3 localPos)
    {
        if (Random.value > decorationDensity) return;

        Vector3 height = new Vector3(0, 0.2525f, 0);
        Vector3 randomXZ = new Vector3(Random.Range(0f, 4f), 0, Random.Range(0f, 4f));
        localPos = localPos + height + randomXZ;

        SpawnProp(GetRandom(centerProps), localPos, Random.Range(0, 4) * 90, null);
    }

    void SpawnProp(GameObject prefab, Vector3 localPos, float rotY, Material mat)
    {
        if (prefab == null) return;
        Vector3 worldPos = transform.TransformPoint(localPos);
        Quaternion worldRot = transform.rotation * Quaternion.Euler(0, rotY, 0);
        GameObject p = Instantiate(prefab, worldPos, worldRot, transform);
        ApplyMaterial(p, mat);
    }

    GameObject GetRandom(GameObject[] list)
    {
        if (list.Length == 0) return null;
        return list[Random.Range(0, list.Length)];
    }

    void ApplyMaterial(GameObject obj, Material mat)
    {
        if (mat == null) return;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.material = mat;
    }

    // Removes unnecessary walls that might overlap due to gap filling
    void CleanupDuplicateWalls()
    {
        List<Transform> walls = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.Contains(wallStraight.name)) walls.Add(child);
        }

        for (int i = walls.Count - 1; i >= 0; i--)
        {
            if (walls[i] == null) continue;
            for (int j = i - 1; j >= 0; j--)
            {
                if (walls[j] == null) continue;
                Transform wallA = walls[i];
                Transform wallB = walls[j];

                float dist = Vector3.Distance(wallA.position, wallB.position);
                float angle = Quaternion.Dot(wallA.rotation, wallB.rotation);

                if (dist < 0.1f && Mathf.Abs(angle) > 0.9f)
                {
                    DestroyImmediate(wallA.gameObject);
                    break;
                }
            }
        }
    }
}