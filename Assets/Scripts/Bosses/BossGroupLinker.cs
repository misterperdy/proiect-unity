using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossGroupLinker : MonoBehaviour
{
    public List<GameObject> bosses;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (bosses.Count > 0)
        {
            bosses.RemoveAll(item => item == null);
        }

        if (bosses.Count == 0)
        {
            Destroy(gameObject);
        }
    }
}
