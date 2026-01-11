using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    // Singleton for easy access from the Teleporter object
    public static MinimapController Instance;

    [Header("References")]
    public RectTransform mapContainer;
    public GameObject nodePrefab;
    public Transform playerTransform;
    public RectTransform minimapFrame;
    public RectTransform playerIcon;
    public GameObject enemyIconPrefab;
    public GameObject teleportMessageUI; // Optional: Text saying "Select Destination"

    [Header("Colors")]
    public Color roomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color hallwayColor = new Color(0.6f, 0.8f, 1f, 1f);

    [Header("Config")]
    public float uiTileSize = 40f;
    public int radius = 3;

    [Header("Zoom Settings")]
    public float minZoom = 0.5f; // How far out you can see
    public float maxZoom = 2.0f; // How close you can zoom
    public float zoomSpeed = 0.1f;
    private float currentZoom = 1.0f;

    private static Dictionary<int, Dictionary<Vector2Int, int>> globalExplorationData = new Dictionary<int, Dictionary<Vector2Int, int>>();

    private int currentLevelIndex = 0;
    private Dictionary<Vector2Int, MinimapNode> currentGridNodes = new Dictionary<Vector2Int, MinimapNode>();

    public float worldTileSize;
    public Vector3 currentLevelOffset;
    private bool isInitialized = false;
    private Vector2Int lastGridPos = new Vector2Int(-999, -999);

    private bool isFullscreen = false;
    public bool IsFullscreen => isFullscreen;

    private bool isTeleportMode = false; // Are we selecting a destination?
    public bool IsTeleportMode => isTeleportMode;

    private Vector2 fixedCenterPosition;
    private Vector2 fixedCenterUIPos;

    // UI Restore vars
    private Vector2 startSize;
    private Vector3 startPos;
    private Vector2 startAnchorMin, startAnchorMax, startPivot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (minimapFrame != null)
        {
            startSize = minimapFrame.sizeDelta;
            startPos = minimapFrame.anchoredPosition;
            startAnchorMin = minimapFrame.anchorMin;
            startAnchorMax = minimapFrame.anchorMax;
            startPivot = minimapFrame.pivot;
        }
        if (teleportMessageUI) teleportMessageUI.SetActive(false);
    }

    public void InitializeMinimap(int[,] grid, int gridSize, float worldTileSize, Vector3 levelOffset, int levelIndex, List<Vector3> roomCenters)
    {
        if (mapContainer == null || nodePrefab == null) return;

        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        currentGridNodes.Clear();

        this.worldTileSize = worldTileSize;
        this.currentLevelOffset = levelOffset;
        this.currentLevelIndex = levelIndex;
        lastGridPos = new Vector2Int(-999, -999);

        currentZoom = 1.0f;
        mapContainer.localScale = Vector3.one;

        if (!globalExplorationData.ContainsKey(levelIndex))
        {
            globalExplorationData[levelIndex] = new Dictionary<Vector2Int, int>();
        }

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                int type = grid[x, y];
                if (type != 1 && type != 2 && type != 3) continue;

                GameObject newNode = Instantiate(nodePrefab, mapContainer);
                RectTransform rt = newNode.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x * uiTileSize, y * uiTileSize);

                MinimapNode nodeScript = newNode.GetComponent<MinimapNode>();
                Color c = (type == 2) ? hallwayColor : roomColor;

                // 1. Calculate Grid World Position
                float worldX = (x * worldTileSize) + levelOffset.x;
                float worldZ = (y * worldTileSize) + levelOffset.z;
                Vector3 specificTilePos = new Vector3(worldX + (worldTileSize / 2), playerTransform.position.y, worldZ + (worldTileSize / 2));

                // 2. Determine Teleport Target
                Vector3 teleportTarget = specificTilePos;

                // If it is a ROOM tile (1) or DOOR tile (3), snap to the nearest Room Center
                // This ensures clicking anywhere in the room sends you to the middle
                if ((type == 1 || type == 3) && roomCenters != null && roomCenters.Count > 0)
                {
                    teleportTarget = GetClosestPoint(specificTilePos, roomCenters);
                    // Ensure Y matches player so they don't spawn in floor
                    teleportTarget.y = playerTransform.position.y;
                }

                nodeScript.Initialize(c, type, teleportTarget, this);

                Vector2Int pos = new Vector2Int(x, y);
                currentGridNodes.Add(pos, nodeScript);

                if (globalExplorationData[levelIndex].ContainsKey(pos))
                {
                    int savedState = globalExplorationData[levelIndex][pos];
                    if (savedState == 2) nodeScript.ShowVisited();
                    else if (savedState == 1) nodeScript.ShowDiscovered();
                }
            }
        }
        isInitialized = true;
    }

    Vector3 GetClosestPoint(Vector3 source, List<Vector3> targets)
    {
        Vector3 closest = source;
        float minDst = float.MaxValue;
        foreach (Vector3 t in targets)
        {
            // Ignore Y difference for distance check
            float d = Vector2.Distance(new Vector2(source.x, source.z), new Vector2(t.x, t.z));
            if (d < minDst)
            {
                minDst = d;
                closest = t;
            }
        }
        return closest;
    }

    private void Update()
    {
        if (!isInitialized || playerTransform == null) return;

        float rawGridX = (playerTransform.position.x - currentLevelOffset.x) / worldTileSize;
        float rawGridY = (playerTransform.position.z - currentLevelOffset.z) / worldTileSize;
        Vector2 playerUIPos = new Vector2(rawGridX * uiTileSize, rawGridY * uiTileSize);

        Vector2 targetAnchorPos = -playerUIPos * currentZoom;

        if (isFullscreen)
        {
            // A. Zoom Logic
            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0)
            {
                
                currentZoom += scroll * zoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
                mapContainer.localScale = Vector3.one * currentZoom;

                mapContainer.anchoredPosition = targetAnchorPos; 
            }

            mapContainer.anchoredPosition = Vector2.Lerp(
                mapContainer.anchoredPosition,
                targetAnchorPos,
                Time.deltaTime * 15f 
            );
        }
        else 
        {
            
            mapContainer.anchoredPosition = Vector2.Lerp(mapContainer.anchoredPosition, -playerUIPos, Time.deltaTime * 10f);
        }
   
        UpdateDiscovery(playerUIPos);

        if (Input.GetKeyDown(KeyCode.Tab) && !isTeleportMode) SetFullscreen(true);
        if (Input.GetKeyUp(KeyCode.Tab) && !isTeleportMode) SetFullscreen(false);
    }

    // --- TELEPORT LOGIC ---

    public void OpenTeleportMap()
    {
        isTeleportMode = true;
        SetFullscreen(true);

        // This is the player's UI coordinate
        float rawGridX = (playerTransform.position.x - currentLevelOffset.x) / worldTileSize;
        float rawGridY = (playerTransform.position.z - currentLevelOffset.z) / worldTileSize;
        fixedCenterPosition = new Vector2(rawGridX * uiTileSize, rawGridY * uiTileSize);

        // Reset zoom state before opening
        currentZoom = 1.0f;
        mapContainer.localScale = Vector3.one;
        mapContainer.anchoredPosition = -fixedCenterPosition;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (teleportMessageUI) teleportMessageUI.SetActive(true);

        foreach (var node in currentGridNodes.Values)
        {
            node.EnableTeleportInteraction(true);
        }
    }

    private void UpdateDiscovery(Vector2 playerUIPos)
    {
        if (playerIcon != null) playerIcon.localRotation = Quaternion.Euler(0, 0, -playerTransform.eulerAngles.y);

        int gridX = Mathf.RoundToInt(playerUIPos.x / uiTileSize);
        int gridY = Mathf.RoundToInt(playerUIPos.y / uiTileSize);
        Vector2Int currentGridPos = new Vector2Int(gridX, gridY);

        if (currentGridPos != lastGridPos)
        {
            DiscoverArea(currentGridPos);
            lastGridPos = currentGridPos;
        }
    }

    public void CloseTeleportMap()
    {
        isTeleportMode = false;

        // Reset the map container's scale for the minimap view
        currentZoom = 1.0f;
        mapContainer.localScale = Vector3.one;

        foreach (var node in currentGridNodes.Values)
        {
            node.EnableTeleportInteraction(false);
        }

        SetFullscreen(false);
        if (teleportMessageUI) teleportMessageUI.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExecuteTeleport(Vector3 targetPos)
    {
        StartCoroutine(TeleportRoutine(targetPos));
    }

    IEnumerator TeleportRoutine(Vector3 targetPos)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.normalTeleporterSfx);
        }

        // 1. Disable Controls / Physics
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();

        if (cc) cc.enabled = false;
        if (rb) rb.isKinematic = true;

        // 2. Wait for end of frame (Ensures button click event finishes)
        yield return new WaitForEndOfFrame();

        // 3. Move
        playerTransform.position = targetPos;

        // 4. Close UI
        CloseTeleportMap();

        // 5. Short Delay to prevent physics glitches in new room
        yield return new WaitForSeconds(0.2f); // <--- THE DELAY YOU REQUESTED

        // 6. Restore
        if (rb) rb.isKinematic = false;
        if (cc) cc.enabled = true;

        // Ensure cursor is correct state
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    // ---------------------

    private void UpdateMinimapState()
    {
        // 1. Calculate Grid Position of Player
        float rawGridX = (playerTransform.position.x - currentLevelOffset.x) / worldTileSize;
        float rawGridY = (playerTransform.position.z - currentLevelOffset.z) / worldTileSize;
        Vector2 rawUIPos = new Vector2(rawGridX * uiTileSize, rawGridY * uiTileSize);

        // 2. Position the Map Container
        if (isFullscreen)
        {
            // Do nothing here. Position is handled by the Zoom logic in Update()
        }
        else // Mini-map mode
        {
            // Follow the player (standard minimized logic)
            mapContainer.anchoredPosition = Vector2.Lerp(mapContainer.anchoredPosition, -rawUIPos, Time.deltaTime * 10f);
        }

        // 3. Update Player Icon Rotation and Discovery Logic
        if (playerIcon != null) playerIcon.localRotation = Quaternion.Euler(0, 0, -playerTransform.eulerAngles.y);

        int gridX = Mathf.RoundToInt(rawGridX);
        int gridY = Mathf.RoundToInt(rawGridY);
        Vector2Int currentGridPos = new Vector2Int(gridX, gridY);

        if (currentGridPos != lastGridPos)
        {
            DiscoverArea(currentGridPos);
            lastGridPos = currentGridPos;
        }
    }

    private void UpdateMapContainerPosition(Vector2 centerUIPos, bool onZoom)
    {

        Vector2 targetAnchor = -centerUIPos * currentZoom;

        if (onZoom)
        {
            mapContainer.anchoredPosition = targetAnchor;
        }
        else // Opening the map
        {
            // When opening, we move the container to center the player at scale 1.0
            mapContainer.anchoredPosition = Vector2.Lerp(mapContainer.anchoredPosition, -centerUIPos, Time.deltaTime * 10f);
        }
    }

    private void DiscoverArea(Vector2Int center)
    {
        if (currentGridNodes.ContainsKey(center) && currentGridNodes[center].tileType == 1 && !currentGridNodes[center].isVisited)
        {
            RevealEntireRoom(center);
        }

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int pos = center + new Vector2Int(x, y);

                if (currentGridNodes.ContainsKey(pos))
                {
                    MinimapNode node = currentGridNodes[pos];
                    float dist = Vector2Int.Distance(center, pos);
                    int newState = 0;

                    if (dist < 1.5f)
                    {
                        node.ShowVisited();
                        newState = 2;
                    }
                    else
                    {
                        node.ShowDiscovered();
                        newState = 1;
                    }
                    SaveTileState(currentLevelIndex, pos, newState);
                }
            }
        }
    }

    void RevealEntireRoom(Vector2Int startNode)
    {
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(startNode);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        visited.Add(startNode);

        while (q.Count > 0)
        {
            Vector2Int curr = q.Dequeue();
            if (currentGridNodes.ContainsKey(curr))
            {
                currentGridNodes[curr].ShowVisited();
                SaveTileState(currentLevelIndex, curr, 2);

                if (currentGridNodes[curr].tileType == 1)
                {
                    Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                    foreach (var d in dirs)
                    {
                        Vector2Int next = curr + d;
                        if (!visited.Contains(next) && currentGridNodes.ContainsKey(next) && currentGridNodes[next].tileType == 1)
                        {
                            visited.Add(next);
                            q.Enqueue(next);
                        }
                    }
                }
            }
        }
    }

    private void SaveTileState(int levelIdx, Vector2Int pos, int newState)
    {
        if (!globalExplorationData.ContainsKey(levelIdx))
            globalExplorationData[levelIdx] = new Dictionary<Vector2Int, int>();

        if (!globalExplorationData[levelIdx].ContainsKey(pos))
        {
            globalExplorationData[levelIdx][pos] = newState;
        }
        else
        {
            int oldState = globalExplorationData[levelIdx][pos];
            if (newState > oldState)
            {
                globalExplorationData[levelIdx][pos] = newState;
            }
        }
    }

    private void SetFullscreen(bool active)
    {
        if (isFullscreen == active && !isTeleportMode) return;

        isFullscreen = active;

        if (isFullscreen)
        {
            float rawGridX = (playerTransform.position.x - currentLevelOffset.x) / worldTileSize;
            float rawGridY = (playerTransform.position.z - currentLevelOffset.z) / worldTileSize;
            fixedCenterPosition = new Vector2(rawGridX * uiTileSize, rawGridY * uiTileSize);

            // 2. Snap the map to its new centered position instantly (no lerp)
            mapContainer.anchoredPosition = -fixedCenterPosition; // Scale 1.0 snap

            // 3. Apply Fullscreen Rect settings
            minimapFrame.anchorMin = new Vector2(0.5f, 0.5f);
            minimapFrame.anchorMax = new Vector2(0.5f, 0.5f);
            minimapFrame.pivot = new Vector2(0.5f, 0.5f);
            minimapFrame.anchoredPosition = Vector2.zero;

            if (minimapFrame.parent != null)
            {
                RectTransform parentRect = minimapFrame.parent as RectTransform;
                minimapFrame.sizeDelta = new Vector2(parentRect.rect.width, parentRect.rect.height);
            }
        }
        else
        {
            // Reset Zoom when closing map
            currentZoom = 1.0f;
            mapContainer.localScale = Vector3.one;

            minimapFrame.anchorMin = startAnchorMin;
            minimapFrame.anchorMax = startAnchorMax;
            minimapFrame.pivot = startPivot;
            minimapFrame.anchoredPosition = startPos;
            minimapFrame.sizeDelta = startSize;
        }
    }
}