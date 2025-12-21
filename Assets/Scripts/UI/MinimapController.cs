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
    public Color hallwayColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    //there is also door type in the map matrix but we set it as hallway color to look better in the minimap

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
                if (type == 0) continue;

                //if not empty,determine color

                Color targetColor = roomColor; // type 1 - room
                if(type == 2 || type == 3)
                {
                    targetColor = hallwayColor; // type 2(hallway) , type 3(door) - hallway color
                }

                //instatitnate on UI
                GameObject newNode = Instantiate(nodePrefab, mapContainer);
                RectTransform rt = newNode.GetComponent<RectTransform>();

                //position
                rt.anchoredPosition = new Vector2(x * uiTileSize, y * uiTileSize);

                //init script
                MinimapNode nodeScript = newNode.GetComponent<MinimapNode>();

                nodeScript.Initialize(targetColor);

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
        for(int x = -radius; x <= radius; x++)
        {
            for(int y = -radius; y <= radius; y++)
            {
                Vector2Int posToCheck = center + new Vector2Int(x, y);

                if (gridNodes.ContainsKey(posToCheck))
                {
                    float dist = Vector2Int.Distance(center, posToCheck);

                    //decide whta to do 
                    if(dist < 1.5f)
                    {
                        gridNodes[posToCheck].ShowVisited();
                    }
                    else
                    {
                        gridNodes[posToCheck].ShowDiscovered();
                    }
                }
            }
        }
    }
}
