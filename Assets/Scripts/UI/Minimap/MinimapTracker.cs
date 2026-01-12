using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapTracker : MonoBehaviour
{
    private MinimapController minimap;
    private RectTransform myIcon;
    private GameObject iconObj;

    public float showDistance = 35f;


    [Header("animation settings")]
    public float animSpeed = 8f;
    private bool isDying = false;

    private bool hasAppearedOnce = false;

    void Start()
    {
        minimap = FindObjectOfType<MinimapController>();

        // create the icon on the minimap container
        if (minimap != null && minimap.enemyIconPrefab != null)
        {
            iconObj = Instantiate(minimap.enemyIconPrefab, minimap.mapContainer);
            myIcon = iconObj.GetComponent<RectTransform>();

            myIcon.SetAsLastSibling(); // render above everything else

            // safety reset z position
            myIcon.localPosition = new Vector3(myIcon.localPosition.x, myIcon.localPosition.y, 0f);

            StartCoroutine(AnimateScale(Vector3.zero, Vector3.one));
        }
    }

    // update loop for tracking position
    void LateUpdate()
    {
        if (myIcon == null || minimap == null || isDying) return;

        // calculate position on map relative to level start
        Vector3 currentPos = transform.position;

        float gridX = (currentPos.x - minimap.currentLevelOffset.x) / minimap.worldTileSize;
        float gridY = (currentPos.z - minimap.currentLevelOffset.z) / minimap.worldTileSize;

        Vector2 uiPos = new Vector2(gridX * minimap.uiTileSize, gridY * minimap.uiTileSize);
        myIcon.anchoredPosition = uiPos; // update ui position

        // check distance to player to hide if too far
        float distToPlayer = Vector3.Distance(transform.position, minimap.playerTransform.position);
        if (distToPlayer > showDistance)
        {
            iconObj.SetActive(false);
        }
        else
        {
            iconObj.SetActive(true);
            if (!hasAppearedOnce)
            {
                StartCoroutine(AnimateScale(Vector3.zero, Vector3.one));
                hasAppearedOnce = true;
            }
            else
            {
                // myIcon.localScale = Vector3.one;
            }
        }
    }

    public void TriggerDeathAnimation()
    {
        if (isDying || iconObj == null) return;

        isDying = true;

        StartCoroutine(AnimateDeath());
    }

    IEnumerator AnimateScale(Vector3 startScale, Vector3 endScale)
    {
        float t = 0f;
        myIcon.localScale = startScale;

        while (t < 1f)
        {
            t += Time.deltaTime * animSpeed;
            // smooth step looks better than lerp
            myIcon.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        myIcon.localScale = endScale;
    }

    IEnumerator AnimateDeath()
    {
        // shrink icon before destroying
        yield return StartCoroutine(AnimateScale(myIcon.localScale, Vector3.zero));

        Destroy(iconObj);

        Destroy(this);
    }

    private void OnDestroy()
    {
        // cleanup icon when enemy dies
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