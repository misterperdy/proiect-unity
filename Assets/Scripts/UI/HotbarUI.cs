using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//script that manages the displaying of your inventory on the UI hotbar
public class InventoryUI : MonoBehaviour
{
    public InventoryManager inventoryManager;

    public Image[] slotItemImages; // drag slots' children in inspector
    public Image[] selectedHotbarImages; //border of each box that is displayed if its the slot selected

    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        inventoryManager.OnInventoryChanged += UpdateUI; // when inventory manager calls OnInventoryChanged, it triggers UpdateUI
        inventoryManager.OnActiveSlotChanged += UpdateSelectedSlot; // update slot selected

        UpdateUI();
        UpdateSelectedSlot(0);
    }

    private void OnDestroy()
    {
        inventoryManager.OnInventoryChanged -= UpdateUI;
    }

    void UpdateUI()
    {
        //iterate through all slots
        for(int i = 0; i < slotItemImages.Length; i++)
        {
            //check if we have item on this slot
            if (i < inventoryManager.items.Count)
            {
                slotItemImages[i].sprite = inventoryManager.items[i].itemIcon;
                slotItemImages[i].gameObject.SetActive(true);
            }
            else
            {
                slotItemImages[i].gameObject.SetActive(false); // disable if we dont have an icon
            }
        }
    }

    //update which hotbar slot displays as selected
    void UpdateSelectedSlot(int activeIndex)
    {
        //iterate through all hotbar slots
        for(int i = 0; i < selectedHotbarImages.Length; i++)
        {
            //if this slot is selected
            if(i == activeIndex)
            {
                selectedHotbarImages[i].gameObject.SetActive(true); //enable the border
            }
            else // else disable the border
            {
                selectedHotbarImages[i].gameObject.SetActive(false);
            }
        }
    }
}
