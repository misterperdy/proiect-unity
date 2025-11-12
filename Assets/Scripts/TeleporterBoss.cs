using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TeleporterBoss : MonoBehaviour
{
    public Transform player, destination, receiver;// Player, Destinatie si portalul la care ajunge
    public float teleportCooldown = 1f;//Timpul de cooldown 

    private bool playerInZone = false; 

    private void Update()
    {
        //Verificare pentru teleportare
        if (playerInZone  && Input.GetButtonDown("Teleport"))
        {
            StartCoroutine(TeleportPlayer());
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
    
    private IEnumerator TeleportPlayer()
    {
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
    }
}

