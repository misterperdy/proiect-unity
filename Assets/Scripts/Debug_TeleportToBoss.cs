using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_TeleportToBoss : MonoBehaviour
{
    public KeyCode teleportKey = KeyCode.B;
    void Update()
    {
        if (Input.GetKeyDown(teleportKey))
        {
            JumpToBoss();
        }
    }

    void JumpToBoss()
    {
        if (DungeonGenerator.instance == null)
        {
            return;
        }

        Vector3 bossPos = DungeonGenerator.instance.currentBossPosition;

        if (bossPos == Vector3.zero)
        {
            return;
        }

        Transform player = DungeonGenerator.instance.player;
        if (player == null) return;


        Vector3 safeLocation = bossPos + new Vector3(0, 2.0f, 0);

        player.position = safeLocation;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.position = safeLocation;
        }
    }
}
