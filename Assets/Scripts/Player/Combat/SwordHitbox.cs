using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    // ref refernce to the attack script
    public PlayerAttack playerAttack;
    public LayerMask enemyLayer;

    private void OnTriggerEnter(Collider other)
    {
        // check if the layer is enemy layer with bitwise math
        if ((enemyLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            // tell player attack to hit the enemy
            playerAttack.RegisterHit(other);
        }
    }
}