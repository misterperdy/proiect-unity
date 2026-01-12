using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// script to show items in hotbar
public class InventoryUI : MonoBehaviour
{
    public InventoryManager inventoryManager;

    public Image[] slotItemImages; // drag drag children
    public Image[] selectedHotbarImages; // borders for selected

    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        inventoryManager.OnInventoryChanged += UpdateUI; // subscribe to event
        inventoryManager.OnActiveSlotChanged += UpdateSelectedSlot; // subscribe to select event

        UpdateUI();
        UpdateSelectedSlot(0); // select first one default
    }

    private void OnDestroy()
    {
        // unsubscribe to be safe
        inventoryManager.OnInventoryChanged -= UpdateUI;
    }

    void UpdateUI()
    {
        // check all slots
        for (int i = 0; i < slotItemImages.Length; i++)
        {
            // if we have item in list
            if (i < inventoryManager.items.Count)
            {
                slotItemImages[i].sprite = inventoryManager.items[i].itemIcon;
                slotItemImages[i].gameObject.SetActive(true);
            }
            else
            {
                slotItemImages[i].gameObject.SetActive(false); // hide if empty
            }
        }
    }

    // show border for selected item
    void UpdateSelectedSlot(int activeIndex)
    {
        // check all borders
        for (int i = 0; i < selectedHotbarImages.Length; i++)
        {
            // if index matches active index
            if (i == activeIndex)
            {
                selectedHotbarImages[i].gameObject.SetActive(true); // show
            }
            else // hide others
            {
                selectedHotbarImages[i].gameObject.SetActive(false);
            }
        }
    }
}