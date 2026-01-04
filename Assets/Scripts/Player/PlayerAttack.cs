using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

// Manages all player attack logic, including switching between melee and ranged,
// handling weapon visuals, and managing per-weapon cooldowns.
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

    [Header("Ranged Attack Settings")]
    public GameObject bowVisual;
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public float arrowSpeed = 30f;

    public GameObject explosionPrefab; 
    public KeyCode abilityKey = KeyCode.E;
    public float explosionCooldown = 6f; 
    private float explosionCooldownEndTime;

    // Instead of a single boolean, we track the cooldown end time for each inventory slot individually.
    private float[] slotCooldownEndTimes;

    private ItemSO currentItem;
    private SwordHitbox swordHitbox;
    private List<Collider> enemiesHitThisSwing;
    private Animator animator;

    private Coroutine activeSwingCoroutine;

    // Stores the player's rotation at the start of a melee attack to ensure the swing is not affected by mouse movement during the animation.
    private Quaternion initialAttackRotation;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // Subscribe to the inventory manager's event to know when the active item changes.
        InventoryManager.Instance.OnActiveSlotChanged += UpdateEquippedItem;

        // Initialize the cooldown array to match the inventory size.
        slotCooldownEndTimes = new float[InventoryManager.Instance.inventorySize];

        // The 'true' argument allows finding the component even if the GameObject is inactive at startup.
        swordHitbox = GetComponentInChildren<SwordHitbox>(true);
        if (swordHitbox != null)
        {
            // Give the hitbox a reference back to this script so it can report hits.
            swordHitbox.playerAttack = this;
            swordHitbox.enemyLayer = this.enemyLayer;
        }

        if (meleeWeaponVisual != null) meleeWeaponVisual.SetActive(false);
        if (bowVisual != null) bowVisual.SetActive(false);

        UpdateEquippedItem(0); // Initialize with the item in the first slot.
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks when the object is destroyed.
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

            // Check if the current time is past the cooldown end time for the *currently selected* weapon slot.
            if (Time.time >= slotCooldownEndTimes[activeSlot])
            {
                // If cooldown is over, perform the appropriate attack.
                switch (activeItemType)
                {
                    case ItemType.Melee:
                        PerformMeleeAttack();
                        animator.SetTrigger("t_melee");
                        break;
                    case ItemType.Ranged:
                        PerformRangedAttack();
                        animator.SetTrigger("t_shoot");
                        break;
                }
            }
        }
    }

    // This method is called by the InventoryManager whenever the player changes their active hotbar slot.
    public void UpdateEquippedItem(int newSlotIndex)
    {
        currentItem = InventoryManager.Instance.GetActiveItem();
        activeItemType = (currentItem != null) ? currentItem.itemType : ItemType.None;

        // Toggle weapon visuals based on the type of item equipped.
        if (bowVisual != null) bowVisual.SetActive(activeItemType == ItemType.Ranged);
    }

    private void PerformRangedAttack()
    {
        int activeSlot = InventoryManager.Instance.activeSlotIndex;
        float finalCooldown = currentItem.attackCooldown / currentItem.fireRateMultiplier;
        slotCooldownEndTimes[activeSlot] = Time.time + finalCooldown;

        int projectiles = Mathf.Max(1, currentItem.projectilesPerShot);
        float spread = currentItem.spreadAngle;

        int bounces = currentItem.maxBounces;

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
                arrow.Fire(this.arrowSpeed, currentItem.damage , bounces);
            }
        }
    }
    private void PerformMeleeAttack()
    {
        int activeSlot = InventoryManager.Instance.activeSlotIndex;

        // Set the cooldown end time for this specific slot.
        float finalCooldown = currentItem.attackCooldown / currentItem.fireRateMultiplier;
        slotCooldownEndTimes[activeSlot] = Time.time + finalCooldown;

        if (activeSwingCoroutine != null)
        {
            StopCoroutine(activeSwingCoroutine);
        }

        ResetMeleeVisuals();

        enemiesHitThisSwing = new List<Collider>();
        initialAttackRotation = transform.rotation;

        activeSwingCoroutine = StartCoroutine(AnimateMeleeSwing());
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

                handler.StartExplosion();
            }
            else
            {
                Debug.LogError("[DEBUG FLOW 3] FATAL: ExplosionHandler NU A FOST GĂSIT pe Prefab. Explozia nu pornește.");
            }

        }
        
    }

    // Public method called by the SwordHitbox script when its trigger collides with something on the enemy layer.
    public void RegisterHit(Collider enemyCollider)
    {
        // Prevents hitting the same enemy multiple times with a single swing.
        if (enemiesHitThisSwing.Contains(enemyCollider)) return;

        enemiesHitThisSwing.Add(enemyCollider);

        float currentDamage = (currentItem != null) ? currentItem.damage : 10f;

        //replaced all the checks with interface
        IDamageable damageableTarget = enemyCollider.GetComponent<IDamageable>();

        if (damageableTarget != null)
        {
            damageableTarget.TakeDamage((int)currentDamage);
        }
    }

    // Animates the visual swing of the melee weapon over the attackDuration.
    private IEnumerator AnimateMeleeSwing()
    {
        if (meleeAttackPivot == null || meleeWeaponVisual == null) yield break;

        meleeWeaponVisual.SetActive(true);

        float elapsedTime = 0f;
        bool swingLeftToRight = Random.value > 0.5f; // Randomize swing direction for variety.

        swingLeftToRight = false;

        float startAngle = swingLeftToRight ? -attackAngle / 2 : attackAngle / 2;
        float endAngle = swingLeftToRight ? attackAngle / 2 : -attackAngle / 2;

        //get from animation of melee swing
        attackDuration = 0.47f;

        while (elapsedTime < attackDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / attackDuration;
            float curveProgress = swingCurve.Evaluate(progress); // Use curve for smoother animation.
            float currentAngle = Mathf.Lerp(startAngle, endAngle, curveProgress);

            // By using the captured 'initialAttackRotation' and setting the world rotation (.rotation),
            // the swing's arc is independent of the player's ongoing rotation
            meleeAttackPivot.rotation = initialAttackRotation * Quaternion.Euler(0, currentAngle, 0);

            yield return null; // Wait for the next frame.
        }

        ResetMeleeVisuals();

        activeSwingCoroutine = null;
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
}