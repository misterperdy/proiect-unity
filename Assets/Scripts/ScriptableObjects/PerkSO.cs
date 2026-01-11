using UnityEngine;

public enum PerkType
{
    HealthBoost,
    DamageBoost,
    MultiShot,
    FireRate,
    LuckBoost, // Added Luck as a perk type too!
    BounceArrows,
    DashCooldownMultiplier,
    Vampirism,
    Regeneration,
    ExtraAdaptive
}

public enum PerkRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "New Perk", menuName = "Perk")]
public class PerkSO : ScriptableObject
{
    public string perkName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effect")]
    public PerkType type;
    public float amount;

    [Header("Rarity & RNG")]
    public PerkRarity rarity;

    [Tooltip("Higher number = More likely to appear.")]
    public int baseWeight = 100;

    [Tooltip("If true, you can only pick this once per run.")]
    public bool isOneTimeOnly = false;
}