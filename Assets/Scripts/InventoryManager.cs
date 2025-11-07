using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        if(items.Count >= inventorySize)
        {
            Debug.Log("inventory full!");
            return false; // didn't add item cause there is no space
        }

        int itemIndex = items.Count; // index of array where the item will be added
        items.Add(item); // added

        OnInventoryChanged?.Invoke(); // invoke inventory change , used in UI updating

        if(itemIndex == activeSlotIndex)
        {
            OnActiveSlotChanged?.Invoke(activeSlotIndex); // re-actualize current slot
        }

        return true;
    }

    public void SetActiveSlot(int index)
    {
        //check if index is valid
        if(index < 0 || index >= inventorySize)
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
        if(activeSlotIndex < items.Count)
        {
            return items[activeSlotIndex];
        }

        return null; // empty slot
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
    }
}
