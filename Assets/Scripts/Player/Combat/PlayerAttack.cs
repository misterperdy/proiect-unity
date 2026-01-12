using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.U2D;
using static UnityEditor.Progress;
using static UnityEngine.GraphicsBuffer;

// logic for player attack melee ranged and weapons
public class PlayerAttack : MonoBehaviour
{
    public ItemType activeItemType = ItemType.None;

    [Header("Melee Attack Settings")]
    public Transform meleeAttackPivot;
    public GameObject meleeWeaponVisual;
    public float attackAngle = 160f;
    public float attackDuration = 0.2f;
    public LayerMask enemyLayer;
    public AnimationCurve swingCurve;

    [Header("Melee SFX")]
    public AudioClip meleeSwingSfx;
    public float meleeSwingSfxVolumeMultiplier = 1f;

    [Header("Ranged Attack Settings")]
    public GameObject bowVisual;
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public float arrowSpeed = 30f;

    public GameObject explosionPrefab;
    public KeyCode abilityKey = KeyCode.E;
    public float explosionCooldown = 6f;
    private float explosionCooldownEndTime;

    // track cooldown for each slot
    private float[] slotCooldownEndTimes;

    private ItemSO currentItem;
    private SwordHitbox swordHitbox;
    private List<Collider> enemiesHitThisSwing;
    private Animator animator;
    private PlayerStats stats;

    private Coroutine activeSwingCoroutine;

    [Header("Sync Settings")]
    [Range(0.1f, 1f)]
    public float swingDurationRatio = 0.8f;

    public float GetCurrentItemBaseDamage(float fallback = 10f)
    {
        return currentItem != null ? currentItem.damage : fallback;
    }

    [Header("Turret Limits")]
    public int maxActiveTurrets = 2;
    public float turretCooldown = 8f;

    private float turretCooldownEndTime = 0f;
    private readonly List<GameObject> activeTurrets = new();

    [Header("Equipped Visual Spawn")]
    public Transform equippedVisualParent; // socket hand
    private GameObject equippedVisualInstance;

    [Header("In-hand visuals (existing children under swordLocation)")]
    public GameObject swordInHand;
    public GameObject hammerInHand;
    public GameObject bowInHand;





    // stores rotation so we swing nicely
    private Quaternion initialAttackRotation;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        stats = GetComponent<PlayerStats>();

        if (meleeSwingSfx == null && MusicManager.Instance != null && MusicManager.Instance.playerMeleeSwingSfx != null)
        {
            meleeSwingSfx = MusicManager.Instance.playerMeleeSwingSfx;
        }
        if (meleeSwingSfx == null) meleeSwingSfx = MusicManager.FindClipByName("sfx_sword_swing");

        // event for when slot changes
        InventoryManager.Instance.OnActiveSlotChanged += UpdateEquippedItem;

        // init cooldowns array
        slotCooldownEndTimes = new float[InventoryManager.Instance.inventorySize];

        // find hitbox even if inactive
        swordHitbox = GetComponentInChildren<SwordHitbox>(true);
        if (swordHitbox != null)
        {
            // tell hitbox about this script
            swordHitbox.playerAttack = this;
            swordHitbox.enemyLayer = this.enemyLayer;
        }

        if (meleeWeaponVisual != null) meleeWeaponVisual.SetActive(false);
        if (bowVisual != null) bowVisual.SetActive(false);

        UpdateEquippedItem(0); // init first slot
    }

    private void OnDestroy()
    {
        // unsubscribe event to no memory leaks
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnActiveSlotChanged -= UpdateEquippedItem;
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            UseExplosionAbility();
        }

        if (PauseManager.IsPaused) return;

        if (Input.GetButton("Fire1"))
        {
            if (currentItem == null)
            {
                Debug.Log("you don't have a weapon!");
                return;
            }

            int activeSlot = InventoryManager.Instance.activeSlotIndex;

            // check cooldown for current weapon
            if (Time.time >= slotCooldownEndTimes[activeSlot])
            {
                float finalCooldown = currentItem.attackCooldown / currentItem.fireRateMultiplier;
                slotCooldownEndTimes[activeSlot] = Time.time + finalCooldown;
                // do attack based on type
                switch (activeItemType)
                {
                    case ItemType.Melee:
                        //PerformMeleeAttack();

                        float attackSpeed = (stats != null) ? stats.fireRateMultiplier : 1f;

                        animator.SetFloat("AttackSpeed", attackSpeed);
                        animator.SetTrigger("t_melee");
                        break;
                    case ItemType.Ranged:
                        
                        animator.SetTrigger("t_shoot");
                        break;
                    case ItemType.Turret:
                        useTurret();
                        break;
                }
            }
        }
    }

    public void AE_StartMeleeSwing()
    {
        if (activeItemType != ItemType.Melee) return;

        PerformMeleeAttack();
    }


    public void UpdateEquippedItem(int newSlotIndex)
    {
        currentItem = InventoryManager.Instance.GetActiveItem();
        activeItemType = (currentItem != null) ? currentItem.itemType : ItemType.None;

        // turn off everything first
        if (swordInHand) swordInHand.SetActive(false);
        if (hammerInHand) hammerInHand.SetActive(false);
        if (bowInHand) bowInHand.SetActive(false);

        // handle ranged visual holder
        if (bowVisual != null) bowVisual.SetActive(activeItemType == ItemType.Ranged);

        if (currentItem == null) return;

        // enable visual based on type
        switch (currentItem.itemType)
        {
            case ItemType.Melee:
                if (swordInHand) swordInHand.SetActive(true);
                break;

            case ItemType.Turret:
                if (hammerInHand) hammerInHand.SetActive(true);
                break;

            case ItemType.Ranged:
                if (bowInHand) bowInHand.SetActive(true);
                break;

            default:
                break;
        }
    }

    public void AM_Shoot()
    {
        PerformRangedAttack();
    }


    private void PerformRangedAttack()
    {
        int activeSlot = InventoryManager.Instance.activeSlotIndex;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.playerBowShootSfx);
        }

        float finalDamage = stats.GetModifiedDamage(currentItem.damage);
        int finalProjectiles = stats.GetModifiedProjectileCount(currentItem.projectilesPerShot);

        float finalCooldown = stats.GetModifiedCooldown(currentItem.attackCooldown);
        slotCooldownEndTimes[activeSlot] = Time.time + finalCooldown;

        int projectiles = Mathf.Max(1, finalProjectiles);
        float spread;
        if (finalProjectiles >= 10) spread = (180 * (1 + finalProjectiles / 2f)) / (finalProjectiles);
        else spread = (90 * (1 + finalProjectiles / 2f)) / (12 - finalProjectiles);

        int bounces = (stats != null)
            ? stats.GetModifiedBounceCount(currentItem.maxBounces)
            : currentItem.maxBounces;

        for (int i = 0; i < projectiles; i++)
        {
            float angleOffset = 0f;

            if (projectiles > 1)
            {
                float totalGapAngle = spread;
                float anglePerProjectile = totalGapAngle / (projectiles - 1);
                float offsetFromStart = anglePerProjectile * i;

                angleOffset = offsetFromStart - (spread / 2f);
            }

            GameObject arrowGO = BulletPool.Instance.GetBullet();

            arrowGO.transform.position = arrowSpawnPoint.position;
            arrowGO.transform.rotation = arrowSpawnPoint.rotation * Quaternion.Euler(0, angleOffset, 0);

            Arrow arrow = arrowGO.GetComponent<Arrow>();

            if (arrow != null)
            {
                arrow.Fire(this.arrowSpeed, finalDamage, bounces, stats);
            }
        }
    }
    private void PerformMeleeAttack()
    {
        if (activeSwingCoroutine != null) StopCoroutine(activeSwingCoroutine);

        ResetMeleeVisuals();

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(meleeSwingSfx, meleeSwingSfxVolumeMultiplier);
        }

        enemiesHitThisSwing = new List<Collider>();
        initialAttackRotation = transform.rotation;

        CalculateDynamicDuration();

        activeSwingCoroutine = StartCoroutine(AnimateMeleeSwing());
    }

    private void CalculateDynamicDuration()
    {
        // INDEX 1 = COMBAT LAYER
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(1);

        float animLength = stateInfo.length;

        float speedMultiplier = (stats != null) ? stats.fireRateMultiplier : 1f;

        if (speedMultiplier < 0.1f) speedMultiplier = 1f;

        attackDuration = (animLength / speedMultiplier) * swingDurationRatio;

        //if (attackDuration <= 0.05f) attackDuration = 0.3f;
    }

    void UseExplosionAbility()
    {
        if (Time.time < explosionCooldownEndTime)
        {
            Debug.Log($"Ability on cooldown");
            return;
        }

        ItemSO currentItem = InventoryManager.Instance.GetActiveItem();

        if (currentItem != null && currentItem.itemType == ItemType.Magic)
        {

            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlaySfx(MusicManager.Instance.playerStaffUseSfx);
            }

            if (currentItem.itemPrefab == null)
            {
                return;
            }

            float cooldownDuration = currentItem.attackCooldown > 0 ? currentItem.attackCooldown : explosionCooldown;
            explosionCooldownEndTime = Time.time + cooldownDuration;

            GameObject explosionGO = Instantiate(
                currentItem.itemPrefab,
                transform.position,
                Quaternion.identity
            );

            ExplosionHandler handler = explosionGO.GetComponent<ExplosionHandler>();

            if (handler != null)
            {
                handler.damage = currentItem.explosionDamage;
                handler.radius = currentItem.explosionRadius;
                handler.delay = currentItem.explosionDelay;
                handler.ownerStats = stats;

                handler.StartExplosion();
            }
            else
            {
                Debug.LogError("[DEBUG FLOW 3] FATAL: ExplosionHandler NU A FOST GĂSIT pe Prefab. Explozia nu pornește.");
            }

        }

    }

    // method called by hitbox script
    public void RegisterHit(Collider enemyCollider)
    {
        // dont hit same enemy twice
        if (enemiesHitThisSwing.Contains(enemyCollider)) return;

        enemiesHitThisSwing.Add(enemyCollider);

        float finalDamage = stats.GetModifiedDamage(currentItem.damage);

        float currentDamage = (currentItem != null) ? finalDamage : 10f;

        // replaced checks with interface
        IDamageable damageableTarget = enemyCollider.GetComponent<IDamageable>();

        if (damageableTarget != null)
        {
            int dealt = (int)currentDamage;
            damageableTarget.TakeDamage(dealt);
            if (stats != null) stats.ReportDamageDealt(dealt);
        }
    }

    private IEnumerator AnimateMeleeSwing()
    {
        if (meleeAttackPivot == null || meleeWeaponVisual == null) yield break;

        meleeWeaponVisual.SetActive(true);

        float elapsedTime = 0f;

        

        
        bool swingLeftToRight = false; 
        float startAngle = swingLeftToRight ? -attackAngle / 2 : attackAngle / 2;
        float endAngle = swingLeftToRight ? attackAngle / 2 : -attackAngle / 2;

        while (elapsedTime < attackDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / attackDuration;

            float curveProgress = swingCurve.Evaluate(progress);
            float currentAngle = Mathf.Lerp(startAngle, endAngle, curveProgress);

            meleeAttackPivot.rotation = initialAttackRotation * Quaternion.Euler(0, currentAngle, 0);

            yield return null;
        }

        ResetMeleeVisuals();
    }

    private void ResetMeleeVisuals()
    {
        if (meleeAttackPivot != null)
        {
            meleeAttackPivot.localRotation = Quaternion.identity;
        }

        if (meleeWeaponVisual != null)
        {
            meleeWeaponVisual.SetActive(false);
        }

        activeSwingCoroutine = null;
    }

    void useTurret()
    {
        if (Time.time < turretCooldownEndTime)
        {
            Debug.Log("Turret on cooldown");
            return;
        }

        CleanupTurretList();

        if (activeTurrets.Count >= maxActiveTurrets)
        {
            Debug.Log($"Max turrets reached ({maxActiveTurrets})");
            return;
        }

        ItemSO currentItem = InventoryManager.Instance.GetActiveItem();
        if (currentItem == null || currentItem.itemType != ItemType.Turret) return;
        if (currentItem.itemPrefab == null) return;

        float cooldownDuration = currentItem.attackCooldown > 0 ? currentItem.attackCooldown : turretCooldown;
        turretCooldownEndTime = Time.time + cooldownDuration;

        GameObject turretGO = Instantiate(
            currentItem.itemPrefab,
            transform.position,
            Quaternion.identity
        );

        activeTurrets.Add(turretGO);
        animator.SetTrigger("t_melee");


        TurretHandler handler = turretGO.GetComponent<TurretHandler>();
        if (handler != null)
        {
            // pass stats
            handler.damage = currentItem.damageTurret;
            handler.fireRate = currentItem.fireRateTurret;
            handler.projectiles = currentItem.projectilesperTurret;
            handler.ownerStats = stats;

            handler.StartTurret();
        }
        else
        {
            Debug.LogError("FATAL: TurretHandler missing on turret prefab.");
        }
    }
    private void CleanupTurretList()
    {
        for (int i = activeTurrets.Count - 1; i >= 0; i--)
        {
            if (activeTurrets[i] == null)
                activeTurrets.RemoveAt(i);
        }
    }



}