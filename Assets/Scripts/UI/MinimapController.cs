using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//brain of the minimap

public class MinimapController : MonoBehaviour
{
    [Header("References")]
    public RectTransform mapContainer;
    public GameObject nodePrefab;
    public Transform playerTransform;

    [Header("Colors")]
    public Color roomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color hallwayColor = new Color(0.6f, 0.8f, 1f, 1f);

    [Header("Config")]
    public float uiTileSize = 40f; // leave as in the prefab
    public int radius = 2; //how much you seee ayround you

    private Dictionary<Vector2Int, MinimapNode> gridNodes = new Dictionary<Vector2Int, MinimapNode>();
    private float worldTileSize;
    private bool isInitialized = false;
    private Vector2Int lastGridPos = new Vector2Int(-999, -999);
    private Vector3 currentLevelOffset = Vector3.zero;

    //function to be called by dungeon generator master script
    public void InitializeMinimap(int[,] grid, int gridSize, float worldTileSize, Vector3 levelOffset)
    {
        //destory if we had anything left from debugging/testing
        foreach(Transform child in mapContainer)
        {
            Destroy(child.gameObject);
        }
        gridNodes.Clear();

        this.worldTileSize = worldTileSize;
        this.currentLevelOffset = levelOffset;

        //generate minimap
        for(int x = 0; x < gridSize; x++)
        {
            for(int y = 0; y < gridSize; y++)
            {
                int type = grid[x, y];

                //empty
                if (type != 1 && type != 2 && type != 3) continue;

                //if not empty,determine color

                Color targetColor = roomColor; // type 1 - room
                if(type == 2)
                {
                    targetColor = hallwayColor; // type 2(hallway)
                }

                //type 3-door

                //instatitnate on UI
                GameObject newNode = Instantiate(nodePrefab, mapContainer);
                RectTransform rt = newNode.GetComponent<RectTransform>();

                //position
                rt.anchoredPosition = new Vector2(x * uiTileSize, y * uiTileSize);

                //init script
                MinimapNode nodeScript = newNode.GetComponent<MinimapNode>();

                nodeScript.Initialize(targetColor, type);

                //save in dictionary
                gridNodes.Add(new Vector2Int(x, y), nodeScript);
            }
        }

        isInitialized = true;
        UpdateMinimapState(); // force an update to be sure
    }

    private void Update()
    {
        if(!isInitialized || playerTransform == null)
        {
            return;
        }

        UpdateMinimapState();
    }

    private void UpdateMinimapState()
    {
        //find out player position in matrix
        float rawGridX = (playerTransform.position.x - currentLevelOffset.x) / worldTileSize;
        float rawGridY = (playerTransform.position.z - currentLevelOffset.z) / worldTileSize;

        //move the map opposite direction of player
        Vector2 targetContainerPos = new Vector2(-rawGridX * uiTileSize, -rawGridY * uiTileSize);

        //lerp it for smooothness
        mapContainer.anchoredPosition = Vector2.Lerp(mapContainer.anchoredPosition, targetContainerPos, Time.deltaTime * 10f);

        //update box logic
        int gridX = Mathf.RoundToInt(rawGridX);
        int gridY = Mathf.RoundToInt(rawGridY);
        Vector2Int currentGridPos = new Vector2Int(gridX, gridY);

        if (currentGridPos != lastGridPos)
        {
            DiscoverArea(currentGridPos);
            lastGridPos = currentGridPos;
        }
    }

    private void DiscoverArea(Vector2Int center)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int neighbor = center + new Vector2Int(x, y);

                if (gridNodes.ContainsKey(neighbor))
                {
                    MinimapNode node = gridNodes[neighbor];

                    if (node.tileType == 1 && !node.isDiscovered)
                    {
                        RevealEntireRoom(neighbor);
                    }
                }
            }
        }

        for (int x = -radius; x <= radius; x++)
        {
            for(int y = -radius; y <= radius; y++)
            {
                Vector2Int posToCheck = center + new Vector2Int(x, y);

                if (gridNodes.ContainsKey(posToCheck))
                {
                    MinimapNode node = gridNodes[posToCheck];

                    if (node.isDiscovered) continue;

                    float dist = Vector2Int.Distance(center, posToCheck);

                    if (dist < 1.5f)
                    {
                        node.ShowVisited();
                    }
                    else
                    {
                        node.ShowDiscovered();
                    }
                }
            }
        }
    }

    void RevealEntireRoom(Vector2Int startNode)
    {
        Queue<Vector2Int> nodesToCheck = new Queue<Vector2Int>();
        nodesToCheck.Enqueue(startNode);

        HashSet<Vector2Int> checkedNodes = new HashSet<Vector2Int>();
        checkedNodes.Add(startNode);

        while (nodesToCheck.Count > 0)
        {
            Vector2Int current = nodesToCheck.Dequeue();

            if (gridNodes.ContainsKey(current))
            {
                MinimapNode node = gridNodes[current];
                node.ShowVisited();

                if (node.tileType == 1)
                {
                    Vector2Int[] dirs = {
                        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                    };

                    foreach (Vector2Int d in dirs)
                    {
                        Vector2Int neighborPos = current + d;

                        if (!checkedNodes.Contains(neighborPos) && gridNodes.ContainsKey(neighborPos))
                        {
                            MinimapNode neighborNode = gridNodes[neighborPos];

                            if (neighborNode.tileType == 1)
                            {
                                nodesToCheck.Enqueue(neighborPos);
                                checkedNodes.Add(neighborPos);
                            }
                        }
                    }
                }
            }
        }
    }
}
