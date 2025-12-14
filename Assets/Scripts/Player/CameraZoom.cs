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
        // Find the CameraFollow script in the scene. 
        // We assume there's one active camera with this script.
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
        if (cameraFollow == null) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f)
        {
            // Calculate new offset magnitude
            Vector3 currentOffset = cameraFollow.offset;
            float currentDist = currentOffset.magnitude;
            
            // Invert scroll input so scroll up zooms in (smaller distance)
            float targetDist = currentDist - (scrollInput * zoomSpeed * Time.deltaTime * 10f); // Multiply by 10 for better feel

            // Clamp distance
            targetDist = Mathf.Clamp(targetDist, minZoom, maxZoom);

            // Apply new magnitude while preserving direction
            cameraFollow.offset = currentOffset.normalized * targetDist;
        }
    }
}
