using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public ItemType activeItemType = ItemType.None;

    private void Start()
    {
        InventoryManager.Instance.OnActiveSlotChanged += UpdateEquippedItem;

        UpdateEquippedItem(0); //start with item on first slot
    }

    private void OnDestroy()
    {
        InventoryManager.Instance.OnActiveSlotChanged -= UpdateEquippedItem;
    }

    //when you change a slot, OnActiveSlotChanged runs from InventoryManager, which then runs this update item function
    public void UpdateEquippedItem(int newSlotIndex)
    {
        ItemSO newItem = InventoryManager.Instance.GetActiveItem();

        if(newItem != null)
        {
            activeItemType = newItem.itemType;
            //here you could update damage player gives, his range etc
        }
        else
        {
            activeItemType = ItemType.None;
        }
    }

    private void Update()
    {
        //check if you left click
        if (Input.GetButtonDown("Fire1"))
        {
            //check what item we have equipped
            switch (activeItemType)
            {
                case ItemType.None:
                    Debug.Log("you don't have a weapon!");
                    break;

                case ItemType.Melee:
                    Debug.Log("Melee attack!");
                    break;

                //ranged case

                //magic case
            }
        }
    }
}
