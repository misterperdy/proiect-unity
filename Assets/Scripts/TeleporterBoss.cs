using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TeleporterBoss : MonoBehaviour
{
    public Transform player, destination, receiver;// Player, Destinatie si portalul la care ajunge
    public float teleportCooldown = 1f;//Timpul de cooldown 

    private bool playerInZone = false;
    private bool isTeleporting = false;
    private float cooldownTimer = 0f;

    private void Update()
    {
        if (PauseManager.IsPaused) return;

        // Update local cooldown timer
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
        
        //Verificare pentru teleportare
        if (playerInZone  && Input.GetButtonDown("Teleport") && !isTeleporting)
        {
            if (cooldownTimer <= 0)
            {
                StartCoroutine(TeleportPlayer());
            }
            else
            {
                Debug.Log("Teleport is on cooldown!");
            }
        }
    }

    //Verificare zonei de collide
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            Debug.Log("Player in teleport zone, press F to teleport");
        }
    }
    //Oprirea dupa iesirea din portal de detectare a zonei
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
        }
    }

    // Called by other teleporters when they send a player here
    public void TriggerCooldown()
    {
        cooldownTimer = teleportCooldown;
    }
    
    private IEnumerator TeleportPlayer()
    {
        isTeleporting = true;
        cooldownTimer = teleportCooldown; // Set local cooldown

        // Notify the receiver teleporter to also start its cooldown
        if (receiver != null)
        {
            TeleporterBoss receiverScript = receiver.GetComponent<TeleporterBoss>();
            if (receiverScript != null)
            {
                receiverScript.TriggerCooldown();
            }
            // Fallback: Try to find it in children if the receiver transform is just a container
            else 
            {
                 receiverScript = receiver.GetComponentInChildren<TeleporterBoss>();
                 if (receiverScript != null)
                 {
                     receiverScript.TriggerCooldown();
                 }
            }
        }

        //Oprirea collider pentru a nu te teleporta din greseala la loc de unde ai plecat
        if (receiver != null)
        {
            Collider recCol = receiver.GetComponent<Collider>();
            if (recCol != null)
            {
                recCol.enabled = false;
                Debug.Log($"{receiver.name}: Collider DISABLED");
            }
        }
        //Pozitia playerului la destinatie
        player.position = destination.position;
        //Timpul de cooldown intre teleportari
        yield return new WaitForSeconds(teleportCooldown);
        //Pornire collider
        if (receiver != null)
        {
            Collider recCol = receiver.GetComponent<Collider>();
            if (recCol != null)
            {
                recCol.enabled = true;
                Debug.Log($"{receiver.name}: Collider ENABLED");
            }
        }

        Debug.Log("Teleport ready again!");
        isTeleporting = false;
    }
}

