using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

//manager script for bullet/arrow pool

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance; // singleton instance to access from anywhere

    public GameObject arrowPrefab; // the bullet object to spawn
    public int defaultCapacity = 20; // how many items to start with
    public int maxCapacity = 100; // max limit for memory safety

    private ObjectPool<GameObject> _pool; // unity internal pool system

    private void Awake()
    {
        Instance = this;

        //initialize the pool logic
        _pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(arrowPrefab), // what to do when we need new one
            actionOnGet: (obj) => obj.SetActive(true), // what to do when we take from pool
            actionOnRelease: (obj) => obj.SetActive(false), // what to do when we put back
            actionOnDestroy: (obj) => Destroy(obj), // clean up if we have too many
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxCapacity
            );
    }

    //To get new bulllet from the pool
    public GameObject GetBullet()
    {
        return _pool.Get();
    }

    //optional,can be done directly with SetActive(false)
    // puts the bullet back in the pool for reuse
    public void ReturnBullet(GameObject bullet)
    {
        // safety check if its already disabled
        if (!bullet.activeSelf)
        {
            return;
        }
        _pool.Release(bullet);
    }
}