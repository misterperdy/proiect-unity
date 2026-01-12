using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public float zoomSpeed = 180f;
    public float minZoom = 9f;
    public float maxZoom = 14f;

    private CameraFollow cameraFollow;

    void Start()
    {
        // locating the camera follow script to change its offset
        cameraFollow = FindObjectOfType<CameraFollow>();

        if (cameraFollow == null)
        {
            Debug.LogError("CameraZoom: No CameraFollow script found in scene!");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // disable zoom if map is fullscreen
        if (MinimapController.Instance != null && MinimapController.Instance.IsFullscreen)
        {
            return;
        }

        if (cameraFollow == null) return;

        // reading scroll wheel
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f)
        {
            // logic to change the offset length
            Vector3 currentOffset = cameraFollow.offset;
            float currentDist = currentOffset.magnitude;

            // calculating target distance
            float targetDist = currentDist - (scrollInput * zoomSpeed * Time.deltaTime * 10f);

            // limiting zoom
            targetDist = Mathf.Clamp(targetDist, minZoom, maxZoom);

            // applying back to offset
            cameraFollow.offset = currentOffset.normalized * targetDist;
        }
    }
}