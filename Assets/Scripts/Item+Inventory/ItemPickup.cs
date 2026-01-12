using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// script needed on prefab to be picked up

public class ItemPickup : MonoBehaviour
{
    public ItemSO item; // data for the item

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PICKUP PREFAB] Spawned: {gameObject.name}");

        if (other.CompareTag("Player"))
        {
            // trying to add to player inventory
            bool wasPickedUp = InventoryManager.Instance.AddItem(item);

            Debug.Log(item);

            // if successful play sound and delete object
            if (wasPickedUp)
            {
                if (MusicManager.Instance != null && item != null && item.itemType != ItemType.None)
                {
                    MusicManager.Instance.PlaySfx(MusicManager.Instance.weaponPickupSfx);
                }

                Destroy(gameObject); // poof
            }
        }
    }

    // helper to freeze physics after dropping
    public void StopDropping()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true; // disable physics calc
        }
    }
}