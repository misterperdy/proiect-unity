using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTeleporter : MonoBehaviour
{
    private bool playerInZone = false;

    // Optional: Visual effect when ready
    public GameObject activeVisual;

    private void Start()
    {
        // Ensure this layer is Interactable
        if (activeVisual) activeVisual.SetActive(true);
    }

    private void Update()
    {
        if (playerInZone && Input.GetKeyDown(KeyCode.F))
        {
            if (MinimapController.Instance != null)
            {
                // Check if already in mode to Toggle
                if (MinimapController.Instance.IsTeleportMode)
                {
                    MinimapController.Instance.CloseTeleportMap();
                }
                else
                {
                    MinimapController.Instance.OpenTeleportMap();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            Debug.Log("Press F to Open Map");
            // You can show a UI tooltip here: "Press F to Teleport"
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            // Also close map if player walks away while it's open
            if (MinimapController.Instance != null && MinimapController.Instance.IsTeleportMode)
            {
                MinimapController.Instance.CloseTeleportMap();
            }
        }
    }
}