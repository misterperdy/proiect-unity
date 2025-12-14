using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// to be added on an item's prefab

public class ItemPickup : MonoBehaviour
{
    public ItemSO item; // item's data

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // if the item on the ground collided with a player, try to add it in inventory
            bool wasPickedUp = InventoryManager.Instance.AddItem(item);

            Debug.Log(item);

            if (wasPickedUp)
            {
                Destroy(gameObject); // destroy item on ground
            }
        }
    }
    public void StopDropping()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
        }
    }
}
