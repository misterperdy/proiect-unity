using System.Collections;
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
    public float vampirismPercent = 0f;
    public float regenerationPercentPerSecond = 0f;
    public int extraPerkOptionsBonus = 0;
    public float proximityDamagePercentPerSecond = 0f;

    [Header("Auras")]
    public float proximityDamageRadius = 4f;

    [Header("Aura Visuals")]
    public bool showProximityAuraVisual = true;

    private float vampirismHealRemainder = 0f;
    private float regenerationHealRemainder = 0f;

    private PlayerHealth playerHealth;

    private AuraRingVisual proximityAuraVisual;

    [Header("Luck")]
    public int luck = 0;

    [Header("UI")]
    public PerkHUD perkHUD;

    public List<PerkSO> acquiredPerks = new List<PerkSO>();

    void Start()
    {
        // Auto-find if forgot to drag
        if (perkHUD == null) perkHUD = FindObjectOfType<PerkHUD>();

        StartCoroutine(RegenerationLoop());
        StartCoroutine(ProximityDamageLoop());

        // In case stats are preloaded (debug), ensure visuals match.
        UpdateProximityAuraVisual();
    }

    void Update()
    {
        UpdateProximityAuraVisual();
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
            case PerkType.Vampirism:
                // Amount is a fraction (0.01 = 1%). Stacks additively.
                vampirismPercent += Mathf.Max(0f, perk.amount);
                break;
            case PerkType.Regeneration:
                // Amount is a fraction per second (0.01 = 1% max HP per second). Stacks additively.
                regenerationPercentPerSecond += Mathf.Max(0f, perk.amount);
                break;
            case PerkType.ExtraAdaptive:
                // Amount is additional perk options offered (1 = 3 -> 4). Typically one-time.
                extraPerkOptionsBonus += Mathf.Max(0, Mathf.RoundToInt(perk.amount));
                break;
            case PerkType.ProximityDamageAura:
                // Amount is a fraction per second of enemy max HP (0.02 = 2%). Stacks additively.
                proximityDamagePercentPerSecond += Mathf.Max(0f, perk.amount);
                UpdateProximityAuraVisual();
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

    public void ReportDamageDealt(int damageDealt)
    {
        if (damageDealt <= 0) return;
        if (vampirismPercent <= 0f) return;
        if (playerHealth == null) return;

        // Accumulate fractional healing so 1% works at low damage.
        vampirismHealRemainder += damageDealt * vampirismPercent;

        int healAmount = Mathf.FloorToInt(vampirismHealRemainder);
        if (healAmount <= 0) return;

        vampirismHealRemainder -= healAmount;
        playerHealth.Heal(healAmount);
    }

    private IEnumerator RegenerationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (regenerationPercentPerSecond <= 0f) continue;
            if (playerHealth == null) continue;

            // Heal based on current max health; accumulate fractional healing.
            regenerationHealRemainder += playerHealth.maxHealth * regenerationPercentPerSecond;

            int healAmount = Mathf.FloorToInt(regenerationHealRemainder);
            if (healAmount <= 0) continue;

            regenerationHealRemainder -= healAmount;
            playerHealth.Heal(healAmount);
        }
    }

    private IEnumerator ProximityDamageLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (proximityDamagePercentPerSecond <= 0f) continue;
            if (proximityDamageRadius <= 0f) continue;

            Collider[] hits = Physics.OverlapSphere(transform.position, proximityDamageRadius);
            if (hits == null || hits.Length == 0) continue;

            HashSet<int> damagedInstanceIds = new HashSet<int>();

            foreach (Collider hit in hits)
            {
                if (hit == null) continue;

                // Ignore self
                if (hit.transform != null && hit.transform.root == transform.root) continue;

                if (!TryGetDamageableWithMaxHealth(hit, out IDamageable damageable, out Component damageableComponent, out int maxHealth))
                {
                    continue;
                }

                int id = damageableComponent.GetInstanceID();
                if (damagedInstanceIds.Contains(id)) continue;
                damagedInstanceIds.Add(id);

                int damage = Mathf.RoundToInt(maxHealth * proximityDamagePercentPerSecond);
                if (damage <= 0) damage = 1;

                damageable.TakeDamage(damage);
                ReportDamageDealt(damage);
            }
        }
    }

    private void UpdateProximityAuraVisual()
    {
        bool shouldShow = showProximityAuraVisual && proximityDamagePercentPerSecond > 0f;

        if (!shouldShow)
        {
            if (proximityAuraVisual != null && proximityAuraVisual.gameObject.activeSelf)
                proximityAuraVisual.gameObject.SetActive(false);
            return;
        }

        if (proximityAuraVisual == null)
        {
            GameObject vfx = new GameObject("Decay Aura Visual");
            vfx.transform.SetParent(transform);
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localRotation = Quaternion.identity;
            vfx.transform.localScale = Vector3.one;

            proximityAuraVisual = vfx.AddComponent<AuraRingVisual>();
            proximityAuraVisual.radius = proximityDamageRadius;
        }

        if (!proximityAuraVisual.gameObject.activeSelf)
            proximityAuraVisual.gameObject.SetActive(true);

        proximityAuraVisual.SetRadius(proximityDamageRadius);
    }

    private static bool TryGetDamageableWithMaxHealth(Collider hit, out IDamageable damageable, out Component damageableComponent, out int maxHealth)
    {
        damageable = null;
        damageableComponent = null;
        maxHealth = 0;

        if (hit == null) return false;

        EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
        if (enemy != null)
        {
            damageable = enemy;
            damageableComponent = enemy;
            maxHealth = enemy.maxHealth;
            return maxHealth > 0;
        }

        ShooterEnemy shooter = hit.GetComponentInParent<ShooterEnemy>();
        if (shooter != null)
        {
            damageable = shooter;
            damageableComponent = shooter;
            maxHealth = shooter.maxHealth;
            return maxHealth > 0;
        }

        KamikazeEnemyAI kamikaze = hit.GetComponentInParent<KamikazeEnemyAI>();
        if (kamikaze != null)
        {
            damageable = kamikaze;
            damageableComponent = kamikaze;
            maxHealth = kamikaze.maxHealth;
            return maxHealth > 0;
        }

        SlimeBoss slimeBoss = hit.GetComponentInParent<SlimeBoss>();
        if (slimeBoss != null)
        {
            damageable = slimeBoss;
            damageableComponent = slimeBoss;
            maxHealth = slimeBoss.maxHealth;
            return maxHealth > 0;
        }

        LichBoss lichBoss = hit.GetComponentInParent<LichBoss>();
        if (lichBoss != null)
        {
            damageable = lichBoss;
            damageableComponent = lichBoss;
            maxHealth = lichBoss.maxHealth;
            return maxHealth > 0;
        }

        DashBoss dashBoss = hit.GetComponentInParent<DashBoss>();
        if (dashBoss != null)
        {
            damageable = dashBoss;
            damageableComponent = dashBoss;
            maxHealth = dashBoss.maxHealth;
            return maxHealth > 0;
        }

        return false;
    }
}