using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    // We will assign this reference from the PlayerAttack script
    public PlayerAttack playerAttack;
    public LayerMask enemyLayer;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object we hit is on the correct layer
        // This uses a bitwise operation to see if other.gameObject.layer is in our enemyLayer mask
        if ((enemyLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            // If it is, tell the main attack script to register the hit
            playerAttack.RegisterHit(other);
        }
    }
}