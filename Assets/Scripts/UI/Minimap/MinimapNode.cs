using UnityEngine;
using UnityEngine.UI;

public class MinimapNode : MonoBehaviour
{
    public Image nodeImage;
    public Button btn; // add a button component to your node prefab

    private Color nodeOriginalColor;
    public int tileType;
    public bool isVisited = false;
    private Vector3 realWorldPosition;
    private MinimapController controller;

    public void Initialize(Color color, int type, Vector3 worldPos, MinimapController ctrl)
    {
        nodeImage = GetComponent<Image>();
        btn = GetComponent<Button>();

        // default button state should be disabled so we dont click random things
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
        // make it transparent to hide it
        Color c = nodeOriginalColor;
        c.a = 0f;
        nodeImage.color = c;
    }

    public void ShowDiscovered()
    {
        if (isVisited) return;
        if (nodeImage == null) return;
        // show it a little bit ghosty if just discovered
        Color c = nodeOriginalColor;
        c.a = 0.2f;
        nodeImage.color = c;
    }

    public void ShowVisited()
    {
        isVisited = true;
        if (nodeImage == null) return;
        // full color if we actually went there
        nodeImage.color = nodeOriginalColor;
    }

    // called by controller when opening map for teleport
    public void EnableTeleportInteraction(bool enable)
    {
        // only allow teleporting to rooms type 1 that we have visited
        if (btn != null && tileType == 1 && isVisited)
        {
            btn.interactable = enable;

            // highlight clickable nodes with green
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