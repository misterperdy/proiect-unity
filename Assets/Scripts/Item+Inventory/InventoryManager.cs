using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

// simple singleton manager for player's inventory

public class InventoryManager : MonoBehaviour
{
    //singleton
    public static InventoryManager Instance;

    public List<ItemSO> items = new List<ItemSO>();
    public int inventorySize = 5;

    public event System.Action OnInventoryChanged;
    public event System.Action<int> OnActiveSlotChanged;

    public int activeSlotIndex = 0; // current active slot

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetActiveSlot(0); //have slot 0 selected when we start
    }

    public bool AddItem(ItemSO item)
    {
        // check if we have inventory space
        if (items.Count >= inventorySize)
        {
            Debug.Log("inventory full!");
            return false; // didn't add item cause there is no space
        }

        int itemIndex = items.Count; // index of array where the item will be added
        items.Add(item); // added

        OnInventoryChanged?.Invoke(); // invoke inventory change , used in UI updating

        if (itemIndex == activeSlotIndex)
        {
            OnActiveSlotChanged?.Invoke(activeSlotIndex); // re-actualize current slot
        }

        return true;
    }

    public void SetActiveSlot(int index)
    {
        //check if index is valid
        if (index < 0 || index >= inventorySize)
        {
            return;
        }

        activeSlotIndex = index;

        OnActiveSlotChanged?.Invoke(activeSlotIndex); // send event

        Debug.Log($"slot changed to {index}");
    }

    public ItemSO GetActiveItem()
    {
        //if we have item in this slot
        if (activeSlotIndex < items.Count)
        {
            return items[activeSlotIndex];
        }

        return null; // empty slot
    }

    public ItemSO DropActiveItem(Vector3 dropPosition)
    {
        if (activeSlotIndex < items.Count)
        {
            ItemSO itemToDrop = items[activeSlotIndex];

            items.RemoveAt(activeSlotIndex);

            if (itemToDrop.itemPrefab != null)
            {
                Vector3 safeDropPosition = dropPosition + Vector3.up * 0.1f;

                GameObject droppedGO = Instantiate(itemToDrop.itemPrefab, safeDropPosition, Quaternion.identity);

                Rigidbody rb = droppedGO.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = droppedGO.AddComponent<Rigidbody>();
                }
                rb.isKinematic = false;
                rb.useGravity = true;

                Collider col = droppedGO.GetComponent<Collider>();
                if (col == null)
                {
                    col = droppedGO.AddComponent<BoxCollider>();
                }
                col.isTrigger = true;

                ItemPickup itemPickup = droppedGO.GetComponent<ItemPickup>();
                if (itemPickup == null)
                {
                    itemPickup = droppedGO.AddComponent<ItemPickup>();
                }

                itemPickup.item = itemToDrop;

                Vector3 dropForce = Vector3.up * 0.5f + new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f)).normalized * 0.5f;
                rb.AddForce(dropForce, ForceMode.Impulse);

                StartCoroutine(LockItemHeight(droppedGO, 0.2f));
            }

            OnInventoryChanged?.Invoke();
            OnActiveSlotChanged?.Invoke(activeSlotIndex);

            return itemToDrop;
        }

        return null;
    }

    private IEnumerator LockItemHeight(GameObject droppedItem, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (droppedItem != null)
        {
            ItemPickup itemPickup = droppedItem.GetComponent<ItemPickup>();

            if (itemPickup != null)
            {
                itemPickup.StopDropping();
            }
        }
    }



    //check input for hotbar slot + player item change
    private void Update()
    {
        //2 slots right now
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            InventoryManager.Instance.SetActiveSlot(0); // key 1 = slot 0
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            InventoryManager.Instance.SetActiveSlot(1); // key 2 = slot 1
        }
        if (Input.GetButtonDown("Drop"))
        {
            GameObject playerGO = GameObject.FindWithTag("Player");

            if (playerGO != null)
            {
                Vector3 playerPos = playerGO.transform.position;
                Vector3 playerForward = playerGO.transform.forward;


                Vector3 dropPosition = playerPos - playerForward * 3.0f + Vector3.up * 0.1f;

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
