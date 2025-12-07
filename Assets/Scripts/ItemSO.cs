using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//scriptable object blueprint for items
//to create an item : right click in assets and go to "Create" -> "New Item"

public enum ItemType { None, Melee, Ranged, Magic}; // here we use an enum like a bool but with multiple choices

[CreateAssetMenu(fileName = "Item", menuName = "New Item")]
public class ItemSO : ScriptableObject
{
    //assign in inspector
    public ItemType itemType;
    public Sprite itemIcon; // for GUI
    public GameObject itemPrefab; // for rendering

    public float damage = 10f;

    [Tooltip("The base time in seconds between attacks for this weapon.")]
    public float attackCooldown = 0.5f;


    public float sizeMultiplier = 1f; //for melee
    public float fireRateMultiplier = 1f; //for ranged/magic
    public int projectilesPerShot = 1;
    public float spreadAngle = 0f;
    public int maxBounces = 0;
}
