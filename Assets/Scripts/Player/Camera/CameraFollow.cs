using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// simple script to make camera follow player
public class CameraFollow : MonoBehaviour
{
    public Transform target; // put the player here
    public Vector3 offset = new Vector3(0f, 10f, -5f); // default distance

    void LateUpdate()
    {
        // using lateupdate so we follow after player moved
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}