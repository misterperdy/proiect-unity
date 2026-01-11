using UnityEngine;
using UnityEngine.UI;

public class MinimapNode : MonoBehaviour
{
    public Image nodeImage;
    public Button btn; // Add a Button component to your Node Prefab

    private Color nodeOriginalColor;
    public int tileType;
    public bool isVisited = false;
    private Vector3 realWorldPosition;
    private MinimapController controller;

    public void Initialize(Color color, int type, Vector3 worldPos, MinimapController ctrl)
    {
        nodeImage = GetComponent<Image>();
        btn = GetComponent<Button>();

        // Default button state: Disabled
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.interactable = false;
            btn.onClick.AddListener(OnNodeClicked);
        }

        nodeOriginalColor = color;
        tileType = type;
        realWorldPosition = worldPos;
        controller = ctrl;

        Hide();
    }

    public void Hide()
    {
        if (nodeImage == null) return;
        Color c = nodeOriginalColor;
        c.a = 0f;
        nodeImage.color = c;
    }

    public void ShowDiscovered()
    {
        if (isVisited) return;
        if (nodeImage == null) return;
        Color c = nodeOriginalColor;
        c.a = 0.2f;
        nodeImage.color = c;
    }

    public void ShowVisited()
    {
        isVisited = true;
        if (nodeImage == null) return;
        nodeImage.color = nodeOriginalColor;
    }

    // Called by Controller when opening map for teleport
    public void EnableTeleportInteraction(bool enable)
    {
        // Only allow teleporting to ROOMS (Type 1) that we have VISITED
        if (btn != null && tileType == 1 && isVisited)
        {
            btn.interactable = enable;

            // Optional: Highlight clickable nodes
            if (enable) nodeImage.color = Color.green;
            else nodeImage.color = nodeOriginalColor;
        }
    }

    void OnNodeClicked()
    {
        if (controller != null)
        {
            controller.ExecuteTeleport(realWorldPosition);
        }
    }
}