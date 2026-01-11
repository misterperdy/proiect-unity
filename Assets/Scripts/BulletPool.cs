using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

//manager script for bullet/arrow pool

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance; // singleton 

    public GameObject arrowPrefab;
    public int defaultCapacity = 20;
    public int maxCapacity = 100;

    private ObjectPool<GameObject> _pool;

    private void Awake()
    {
        Instance = this;

        //initialize the pool
        _pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(arrowPrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxCapacity
            );
    }

    //To get new bulllet
    public GameObject GetBullet()
    {
        return _pool.Get();
    }

    //optional,can be done directly with SetActive(false)
    public void ReturnBullet(GameObject bullet)
    {
        if (!bullet.activeSelf)
        {
            return;
        }
        _pool.Release(bullet);
    }
}
