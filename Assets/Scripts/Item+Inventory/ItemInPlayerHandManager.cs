using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemInPlayerHandManager : MonoBehaviour
{
    public InventoryManager inventoryManager;

    public GameObject sword;
    public GameObject bow;

    // Start is called before the first frame update
    private void Awake()
    {
        inventoryManager = GetComponent<InventoryManager>(); // get this inventory manager
    }

    // Update is called once per frame
    void Update()
    {
        if(inventoryManager.activeSlotIndex < inventoryManager.items.Count)
        {
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
                    sword.gameObject.SetActive(false);
                    bow.gameObject.SetActive(false);
                    break;
            }
        }
        else
        {
            sword.gameObject.SetActive(false);
            bow.gameObject.SetActive(false);
        }
        
            

    }
}
