using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTeleporter : MonoBehaviour
{
    private bool playerInZone = false; // check if player is near

    // Optional: Visual effect when ready
    public GameObject activeVisual; // reference to the shiny effect

    private void Start()
    {
        // make sure the visual effect is on when game starts
        if (activeVisual) activeVisual.SetActive(true);
    }

    private void Update()
    {
        // checking if player is close and pressed F key
        if (playerInZone && Input.GetKeyDown(KeyCode.F))
        {
            // accessing the minimap controller to show the map
            if (MinimapController.Instance != null)
            {
                // Check if already in mode to Toggle
                // basically open or close the map menu
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
        // verify if the object entering is the player
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            Debug.Log("Press F to Open Map");
            // showing debug msg, maybe add ui later
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // detecting when player leaves the zone
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            // auto close the map if player walks away too far
            if (MinimapController.Instance != null && MinimapController.Instance.IsTeleportMode)
            {
                MinimapController.Instance.CloseTeleportMap();
            }
        }
    }
}