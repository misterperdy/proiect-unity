using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//script for camera to follow player
public class CameraFollow : MonoBehaviour
{
    public Transform target; // assign player in inspector
    public Vector3 offset = new Vector3(0f, 10f, -5f); // camera distance from player

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }    
    }
}
