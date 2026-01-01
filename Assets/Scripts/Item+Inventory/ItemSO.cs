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

    [Header("Melee")]
    public float attackCooldown = 0.5f;
    public float sizeMultiplier = 1f;

    [Header("Ranged")]
    public float fireRateMultiplier = 1f; 
    public int projectilesPerShot = 1;
    public float spreadAngle = 0f;
    public int maxBounces = 0;

    [Header("Explosion Ability Settings")]
    public int explosionDamage = 40;
    public float explosionRadius = 4f;
    public float explosionDelay = 1.0f;
    public float explosionForce = 1000f;

}
