using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// blueprint for creating items in unity editor

public enum ItemType { None, Melee, Ranged, Magic, Turret }; // list of types

[CreateAssetMenu(fileName = "Item", menuName = "New Item")]
public class ItemSO : ScriptableObject
{
    // basic info
    public ItemType itemType;
    public Sprite itemIcon; // icon for ui
    [Header("Prefabs")]
    public GameObject itemPrefab;      // the object used when attacking or placing
    public GameObject pickupPrefab;    // the object shown on ground when dropped


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

    [Header("Turret Settings")]
    public int damageTurret = 40;
    public float fireRateTurret = 1f;
    public int projectilesperTurret = 1;

    [Header("Equipped Visual (in-hand)")]
    public GameObject equippedVisualPrefab; // optional visual for player hand


}