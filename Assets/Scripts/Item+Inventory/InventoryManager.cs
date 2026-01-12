using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// simple singleton manager for player's inventory

public class InventoryManager : MonoBehaviour
{
    // singleton instance so i can call it from anywhere
    public static InventoryManager Instance;

    public List<ItemSO> items = new List<ItemSO>();
    public int inventorySize = 5;

    public event System.Action OnInventoryChanged;
    public event System.Action<int> OnActiveSlotChanged;

    public int activeSlotIndex = 0; // tracking which slot is selected

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetActiveSlot(0); // start with the first slot
    }

    public bool AddItem(ItemSO item)
    {
        // checking if bag is full
        if (items.Count >= inventorySize)
        {
            Debug.Log("inventory full!");
            return false; // cant add more stuff
        }

        int itemIndex = items.Count; // getting the new index
        items.Add(item); // putting item in list


        OnInventoryChanged?.Invoke(); // tell ui to update

        // if we added item to the slot we are holding, update visuals
        if (itemIndex == activeSlotIndex)
        {
            OnActiveSlotChanged?.Invoke(activeSlotIndex);
        }

        return true;
    }

    public void SetActiveSlot(int index)
    {
        // validation check to dont crash
        if (index < 0 || index >= inventorySize)
        {
            return;
        }

        activeSlotIndex = index;

        OnActiveSlotChanged?.Invoke(activeSlotIndex); // notify everyone slot changed

        Debug.Log($"slot changed to {index}");
    }

    public ItemSO GetActiveItem()
    {
        // safety check if slot is empty
        if (activeSlotIndex < items.Count)
        {
            return items[activeSlotIndex];
        }

        return null; // nothing here
    }

    // logic to throw item on the ground
    public ItemSO DropActiveItem(Vector3 dropPosition)
    {
        // checking if we actually have an item selected
        if (activeSlotIndex < 0 || activeSlotIndex >= items.Count)
            return null;

        ItemSO itemToDrop = items[activeSlotIndex];
        items.RemoveAt(activeSlotIndex); // removing from list

        // logic to decide what prefab to spawn
        // prefer pickupPrefab if it exists, otherwise use the normal itemPrefab
        GameObject dropPrefab = itemToDrop.pickupPrefab != null
            ? itemToDrop.pickupPrefab
            : itemToDrop.itemPrefab;

        Debug.Log($"[DROP] Item: {itemToDrop.name}");
        Debug.Log($"[DROP] itemPrefab: {(itemToDrop.itemPrefab != null ? itemToDrop.itemPrefab.name : "NULL")}");
        Debug.Log($"[DROP] pickupPrefab: {(itemToDrop.pickupPrefab != null ? itemToDrop.pickupPrefab.name : "NULL")}");
        Debug.Log($"[DROP] dropPrefab chosen: {(dropPrefab != null ? dropPrefab.name : "NULL")}");

        // if something is wrong and no prefab exists
        if (dropPrefab == null)
        {
            Debug.LogWarning($"[DROP] No prefab to drop for item: {itemToDrop.name}. Dropping removed it from inventory anyway.");
            OnInventoryChanged?.Invoke();
            OnActiveSlotChanged?.Invoke(activeSlotIndex);
            return itemToDrop;
        }

        // spawning it a bit higher so it falls
        Vector3 safeDropPosition = dropPosition + Vector3.up * 0.1f;
        GameObject droppedGO = Instantiate(dropPrefab, safeDropPosition, Quaternion.identity);
        Debug.Log($"[DROP] droppedGO spawned: {droppedGO.name}");

        // make sure the dropped object has the script to be picked up again
        ItemPickup itemPickup = droppedGO.GetComponent<ItemPickup>();
        if (itemPickup == null) itemPickup = droppedGO.AddComponent<ItemPickup>();
        itemPickup.item = itemToDrop;

        // adding collider if missing so it doesnt fall thru floor
        Collider col = droppedGO.GetComponent<Collider>();
        if (col == null) col = droppedGO.AddComponent<SphereCollider>();
        col.isTrigger = true;

        // adding physics to throw it
        Rigidbody rb = droppedGO.GetComponent<Rigidbody>();
        if (rb == null) rb = droppedGO.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        // giving it a small push in random direction
        Vector3 dropForce =
            Vector3.up * 0.5f +
            new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f)).normalized * 0.5f;

        rb.AddForce(dropForce, ForceMode.Impulse);

        // stop physics after a bit so it doesnt roll forever
        StartCoroutine(LockItemHeight(droppedGO, 0.2f));

        // update ui again
        OnInventoryChanged?.Invoke();
        OnActiveSlotChanged?.Invoke(activeSlotIndex);

        return itemToDrop;
    }


    private IEnumerator LockItemHeight(GameObject droppedItem, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (droppedItem != null)
        {
            ItemPickup itemPickup = droppedItem.GetComponent<ItemPickup>();

            // call the function to freeze rigidbody
            if (itemPickup != null)
            {
                itemPickup.StopDropping();
            }
        }
    }



    // checking keys every frame
    private void Update()
    {
        // simple number keys for slots
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            InventoryManager.Instance.SetActiveSlot(0); // key 1 is slot 0
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            InventoryManager.Instance.SetActiveSlot(1); // key 2 is slot 1
        }

        // checking drop button
        if (Input.GetButtonDown("Drop"))
        {
            GameObject playerGO = GameObject.FindWithTag("Player");

            if (playerGO != null)
            {
                Vector3 playerPos = playerGO.transform.position;
                Vector3 playerForward = playerGO.transform.forward;

                // calc position in front of player
                Vector3 dropPosition = playerPos + playerForward * 3.0f + Vector3.up * 0.1f;

                ItemSO droppedItem = DropActiveItem(dropPosition);

                if (droppedItem != null)
                {
                    Debug.Log($"SUCCESS: Dropped item: {droppedItem.name}");
                }
            }
            else
            {
                Debug.LogError("FATAL DROP ERROR: Player object not found. Ensure your Player has the tag 'Player'.");
            }
        }
    }
}