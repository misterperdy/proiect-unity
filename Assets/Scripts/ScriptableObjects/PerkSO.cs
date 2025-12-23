using UnityEngine;

public enum PerkType
{
    HealthBoost,
    DamageBoost,
    MultiShot,
    FireRate
}

[CreateAssetMenu(fileName = "New Perk", menuName = "Perk")]
public class PerkSO : ScriptableObject
{
    public string perkName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Effect")]
    public PerkType type;

    [Tooltip("For Percentage, use decimals (0.2 = 20%). For Counts, use integers (1 = +1).")]
    public float amount;
}