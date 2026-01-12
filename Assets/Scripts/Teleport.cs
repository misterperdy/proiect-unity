using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    public Transform player, destination, receiver;// references for player and where he goes
    public float teleportCooldown = 1f; // how long to wait before using again

    private bool isTeleporting = false;

    private void OnTriggerEnter(Collider other)
    {
        // checking if player walked into the portal and isnt already teleporting
        if (other.CompareTag("Player") && !isTeleporting)
        {
            Debug.Log("Teleport triggered!");
            StartCoroutine(TeleportPlayer());
        }
    }

    private IEnumerator TeleportPlayer()
    {
        isTeleporting = true; // lock it so it doesnt trigger twice

        if (MusicManager.Instance != null)
        {
            // playing the teleport sound effect
            MusicManager.Instance.PlaySfx(MusicManager.Instance.normalTeleporterSfx);
        }

        // we turn off the collider at the destination so we dont get teleported back immediately
        if (receiver != null)
        {
            Collider recCol = receiver.GetComponent<Collider>();
            if (recCol != null)
            {
                recCol.enabled = false;
                Debug.Log($"{receiver.name}: Collider DISABLED");
            }
        }
        // actually moving the player to the new position
        player.position = destination.position;
        // wait for the cooldown timer
        yield return new WaitForSeconds(teleportCooldown);
        // turn the destination collider back on so it can be used again later
        if (receiver != null)
        {
            Collider recCol = receiver.GetComponent<Collider>();
            if (recCol != null)
            {
                recCol.enabled = true;
                Debug.Log($"{receiver.name}: Collider ENABLED");
            }
        }
        isTeleporting = false; // unlock the portal
    }
}