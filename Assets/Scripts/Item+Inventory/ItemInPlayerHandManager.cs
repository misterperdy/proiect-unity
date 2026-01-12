using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemInPlayerHandManager : MonoBehaviour
{
    public InventoryManager inventoryManager;

    public GameObject sword;
    public GameObject bow;

    // getting reference at start
    private void Awake()
    {
        inventoryManager = GetComponent<InventoryManager>();
    }

    // checking every frame what to show in hand
    void Update()
    {
        // make sure index is valid
        if (inventoryManager.activeSlotIndex < inventoryManager.items.Count)
        {
            // switch visuals based on item type
            switch (inventoryManager.items[inventoryManager.activeSlotIndex].itemType)
            {
                case ItemType.Melee:
                    sword.gameObject.SetActive(true);
                    bow.gameObject.SetActive(false);
                    break;

                case ItemType.Ranged:
                    bow.gameObject.SetActive(true);
                    sword.gameObject.SetActive(false);
                    break;

                default:
                    // hide everything if unknown item
                    sword.gameObject.SetActive(false);
                    bow.gameObject.SetActive(false);
                    break;
            }
        }
        else
        {
            // empty slot means empty hands
            sword.gameObject.SetActive(false);
            bow.gameObject.SetActive(false);
        }



    }
}