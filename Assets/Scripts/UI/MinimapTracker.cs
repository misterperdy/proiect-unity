using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapTracker : MonoBehaviour
{
    private MinimapController minimap;
    private RectTransform myIcon;
    private GameObject iconObj;

    void Start()
    {
        minimap = FindObjectOfType<MinimapController>();

        if(minimap != null && minimap.enemyIconPrefab != null)
        {
            iconObj = Instantiate(minimap.enemyIconPrefab, minimap.mapContainer);
            myIcon = iconObj.GetComponent<RectTransform>();

            myIcon.SetAsLastSibling(); // to render above evveeryhitng

            //for safety:
            myIcon.localPosition = new Vector3(myIcon.localPosition.x, myIcon.localPosition.y, 0f);
            myIcon.localScale = Vector3.one;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (myIcon == null || minimap == null) return;

        //calculate pozition on map
        Vector3 currentPos = transform.position;

        float gridX = (currentPos.x - minimap.currentLevelOffset.x) / minimap.worldTileSize;
        float gridY = (currentPos.z - minimap.currentLevelOffset.z) / minimap.worldTileSize;

        Vector2 uiPos = new Vector2(gridX * minimap.uiTileSize, gridY * minimap.uiTileSize);
        myIcon.anchoredPosition = uiPos; // update on UI
    }

    private void OnDestroy()
    {
        if (iconObj != null)
        {
            Destroy(iconObj);
        }
    }

    void OnDisable()
    {
        if (iconObj != null) iconObj.SetActive(false);
    }

    void OnEnable()
    {
        if (iconObj != null) iconObj.SetActive(true);
    }
}
