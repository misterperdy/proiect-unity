using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationRelay : MonoBehaviour
{
    private PlayerAttack playerAttack;

    private void Awake()
    {
        playerAttack = GetComponentInParent<PlayerAttack>();
    }

    public void AE_StartMeleeSwing()
    {
        if (playerAttack != null)
        {
            playerAttack.AE_StartMeleeSwing();
        }
        else
        {
            Debug.LogError("can't find playerAttack script in parent!");
        }
    }

    public void AM_Shoot()
    {
        if (playerAttack != null)
        {
            playerAttack.AM_Shoot();
        }
        else
        {
            Debug.LogError("can't find playerAttack script in parent!");
        }
    }
}
