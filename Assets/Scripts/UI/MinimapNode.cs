using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//node for minimap
public class MinimapNode : MonoBehaviour
{
    public Image nodeImage;
    public Color nodeOriginalColor;
    public bool isDiscovered = false;

    public void Initialize(Color color)
    {
        nodeImage = GetComponent<Image>();
        nodeOriginalColor = color;

        Hide();
    }

    public void Hide()
    {
        if (nodeImage == null) return;

        Color c = nodeOriginalColor;
        c.a = 0f; // set alpha to 0 == transaprettnt
        nodeImage.color = c;
    }

    public void ShowDiscovered()
    {
        //a little bit visible
        if (isDiscovered) return;

        if (nodeImage == null) return;

        Color c = nodeOriginalColor;
        c.a = 0.3f; // set alpha to 30%
        nodeImage.color = c;
    }

    public void ShowVisited()
    {
        //completely visible

        isDiscovered = true;
        if (nodeImage == null) return;
        nodeImage.color = nodeOriginalColor;
    }
}
