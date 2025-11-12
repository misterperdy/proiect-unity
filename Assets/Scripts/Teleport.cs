using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    public Transform player, destination,receiver;// Player, Destinatie si portalul la care ajunge
    public float teleportCooldown = 1f; //Timpul de cooldown 

    

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") )
        {
            Debug.Log("Teleport triggered!");
            StartCoroutine(TeleportPlayer());
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


    }
}
 