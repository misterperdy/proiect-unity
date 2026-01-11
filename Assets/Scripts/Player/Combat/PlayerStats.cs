using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Modifiers")]
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float fireRateMultiplier = 1f;
    public int bonusProjectiles = 0;
    public int bonusBounces = 0;
    public float dashCooldownMultiplier = 1f;

    private PlayerHealth playerHealth;

    [Header("Luck")]
    public int luck = 0;

    [Header("UI")]
    public PerkHUD perkHUD;

    public List<PerkSO> acquiredPerks = new List<PerkSO>();

    void Start()
    {
        // Auto-find if forgot to drag
        if (perkHUD == null) perkHUD = FindObjectOfType<PerkHUD>();
    }

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    public void ApplyPerk(PerkSO perk)
    {
        acquiredPerks.Add(perk);

        switch (perk.type)
        {
            case PerkType.HealthBoost:
                ApplyHealthBoost(perk.amount);
                break;
            case PerkType.DamageBoost:
                damageMultiplier += perk.amount;
                break;
            case PerkType.MultiShot:
                bonusProjectiles += Mathf.RoundToInt(perk.amount);
                break;
            case PerkType.FireRate:
                fireRateMultiplier += perk.amount;
                break;
            case PerkType.LuckBoost:
                luck += Mathf.RoundToInt(perk.amount);
                break;
            case PerkType.BounceArrows:
                bonusBounces += Mathf.RoundToInt(perk.amount);
                break;
            case PerkType.DashCooldownMultiplier:
                // Amount is a multiplier (0.5 = half cooldown). Clamp to avoid 0/negative.
                float mult = (perk.amount <= 0f) ? 1f : perk.amount;
                dashCooldownMultiplier *= Mathf.Clamp(mult, 0.05f, 10f);
                break;
        }

        if (perkHUD != null) perkHUD.UpdateDisplay();

        Debug.Log($"Applied Perk: {perk.perkName}");
    }

    void ApplyHealthBoost(float percentage)
    {
        if (playerHealth != null)
        {
            // Calculate how much HP to add based on Base Max Health (assuming 100 or current max)
            int amountToAdd = Mathf.RoundToInt(playerHealth.maxHealth * percentage);
            playerHealth.maxHealth += amountToAdd;
            playerHealth.Heal(amountToAdd); // Heal the amount we just gained

            GameObject healthBar = GameObject.Find("HealthBar");
            if (healthBar != null)
            {
                HealthBarUI hbUI = healthBar.GetComponent<HealthBarUI>();
                if (hbUI != null)
                {
                    hbUI.UpdateWidth();
                }
            }
        }
    }

    public bool HasPerk(PerkSO perk)
    {
        return acquiredPerks.Contains(perk);
    }

    public float GetModifiedDamage(float baseDamage)
    {
        return baseDamage * damageMultiplier;
    }

    public int GetModifiedProjectileCount(int baseCount)
    {
        return baseCount + bonusProjectiles;
    }

    public int GetModifiedBounceCount(int baseCount)
    {
        return Mathf.Max(0, baseCount + bonusBounces);
    }

    public float GetModifiedCooldown(float baseCooldown)
    {
        // Higher multiplier = Lower cooldown
        return baseCooldown / fireRateMultiplier;
    }

    public float GetModifiedDashCooldown(float baseCooldown)
    {
        return Mathf.Max(0.01f, baseCooldown * dashCooldownMultiplier);
    }
}