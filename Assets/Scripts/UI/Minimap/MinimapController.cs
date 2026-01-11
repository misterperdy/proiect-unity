using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("References")]
    public RectTransform mapContainer;
    public GameObject nodePrefab;
    public Transform playerTransform;
    public RectTransform minimapFrame;
    public RectTransform playerIcon;
    public GameObject enemyIconPrefab;

    [Header("Colors")]
    public Color roomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    public Color hallwayColor = new Color(0.6f, 0.8f, 1f, 1f);

    [Header("Config")]
    public float uiTileSize = 40f;
    public int radius = 3;

    // --- MEMORIA GLOBAL? ACTUALIZAT? ---
    // Cheie 1: Indexul Nivelului
    // Cheie 2: Coordonata (Vector2Int)
    // Valoare: STAREA (1 = Discovered/Gri Închis, 2 = Visited/Gri Deschis)
    private static Dictionary<int, Dictionary<Vector2Int, int>> globalExplorationData = new Dictionary<int, Dictionary<Vector2Int, int>>();

    // --- VARIABILE INTERNE ---
    private int currentLevelIndex = 0;
    private Dictionary<Vector2Int, MinimapNode> currentGridNodes = new Dictionary<Vector2Int, MinimapNode>();

    public float worldTileSize;
    public Vector3 currentLevelOffset;
    private bool isInitialized = false;
    private Vector2Int lastGridPos = new Vector2Int(-999, -999);

    // Variabile UI Fullscreen
    private bool isFullscreen = false;
    private Vector2 startSize;
    private Vector3 startPos;
    private Vector2 startAnchorMin, startAnchorMax, startPivot;

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
    }

    public void InitializeMinimap(int[,] grid, int gridSize, float worldTileSize, Vector3 levelOffset, int levelIndex)
    {
        // 1. Cur???m harta veche
        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        currentGridNodes.Clear();

        this.worldTileSize = worldTileSize;
        this.currentLevelOffset = levelOffset;
        this.currentLevelIndex = levelIndex;
        lastGridPos = new Vector2Int(-999, -999);

        // Asigur?m dic?ionarul pentru acest nivel
        if (!globalExplorationData.ContainsKey(levelIndex))
        {
            globalExplorationData[levelIndex] = new Dictionary<Vector2Int, int>();
        }

        // 2. Gener?m nodurile UI
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
                nodeScript.Initialize(c, type);

                Vector2Int pos = new Vector2Int(x, y);
                currentGridNodes.Add(pos, nodeScript);

                // --- RESTAURARE INTELIGENT? ---
                // Verific?m ce stare aveam salvat? în memorie
                if (globalExplorationData[levelIndex].ContainsKey(pos))
                {
                    int savedState = globalExplorationData[levelIndex][pos];

                    if (savedState == 2)
                    {
                        nodeScript.ShowVisited(); // Starea 2 = Full Vizibil
                    }
                    else if (savedState == 1)
                    {
                        nodeScript.ShowDiscovered(); // Starea 1 = Doar contur/Gri inchis
                    }
                }
            }
        }
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || playerTransform == null) return;
        UpdateMinimapState();
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            SetFullscreen(true);
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            SetFullscreen(false);
        }
    }

    private void UpdateMinimapState()
    {
        float rawGridX = (playerTransform.position.x - currentLevelOffset.x) / worldTileSize;
        float rawGridY = (playerTransform.position.z - currentLevelOffset.z) / worldTileSize;

        mapContainer.anchoredPosition = Vector2.Lerp(mapContainer.anchoredPosition, new Vector2(-rawGridX * uiTileSize, -rawGridY * uiTileSize), Time.deltaTime * 10f);

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

    private void DiscoverArea(Vector2Int center)
    {
        // 1. Reveal Room Logic (BFS) - Camerele se descoper? complet (Visited = 2)
        if (currentGridNodes.ContainsKey(center) && currentGridNodes[center].tileType == 1 && !currentGridNodes[center].isDiscovered)
        {
            RevealEntireRoom(center);
        }

        // 2. Reveal Radius Logic
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

                    // Determin?m starea curent? bazat? pe distan??
                    if (dist < 1.5f)
                    {
                        node.ShowVisited(); // E?ti pe el -> Gri Deschis
                        newState = 2;
                    }
                    else
                    {
                        node.ShowDiscovered(); // E?ti aproape -> Gri Închis
                        newState = 1;
                    }

                    // --- SALVARE ÎN MEMORIA GLOBAL? ---
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
                // Camerele descoperite prin intrare devin automat "Visited" (Starea 2)
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

    // Func?ie ajut?toare pentru a nu suprascrie "Visited" cu "Discovered"
    private void SaveTileState(int levelIdx, Vector2Int pos, int newState)
    {
        if (!globalExplorationData.ContainsKey(levelIdx))
            globalExplorationData[levelIdx] = new Dictionary<Vector2Int, int>();

        // Dac? nu avem nimic salvat, salv?m noua stare
        if (!globalExplorationData[levelIdx].ContainsKey(pos))
        {
            globalExplorationData[levelIdx][pos] = newState;
        }
        else
        {
            // Dac? avem deja salvat, facem update DOAR dac? noua stare e "mai bun?"
            // 2 (Visited) > 1 (Discovered)
            // Astfel, dac? ai vizitat deja (2), ?i treci pe lâng? (radius vede ca 1), r?mâne 2.
            int oldState = globalExplorationData[levelIdx][pos];
            if (newState > oldState)
            {
                globalExplorationData[levelIdx][pos] = newState;
            }
        }
    }

    private void SetFullscreen(bool active)
    {
        // Dac? starea cerut? e deja activ?, nu facem nimic (evit?m calcule inutile)
        if (isFullscreen == active) return;

        isFullscreen = active;

        if (isFullscreen)
        {
            // --- MOD FULLSCREEN (ACTIVAT) ---
            minimapFrame.anchorMin = new Vector2(0.5f, 0.5f);
            minimapFrame.anchorMax = new Vector2(0.5f, 0.5f);
            minimapFrame.pivot = new Vector2(0.5f, 0.5f);

            minimapFrame.anchoredPosition = Vector2.zero;

            // Ne asigur?m c? p?rintele exist? înainte s? lu?m dimensiunile
            if (minimapFrame.parent != null)
            {
                RectTransform parentRect = minimapFrame.parent as RectTransform;
                minimapFrame.sizeDelta = new Vector2(parentRect.rect.width, parentRect.rect.height);
            }
        }
        else
        {
            // --- MOD MINIMAP (RESTORE) ---
            minimapFrame.anchorMin = startAnchorMin;
            minimapFrame.anchorMax = startAnchorMax;
            minimapFrame.pivot = startPivot;
            minimapFrame.anchoredPosition = startPos;
            minimapFrame.sizeDelta = startSize;
        }
    }
}