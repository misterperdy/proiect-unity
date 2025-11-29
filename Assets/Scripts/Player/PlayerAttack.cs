using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Instead of a single boolean, we track the cooldown end time for each inventory slot individually.
    private float[] slotCooldownEndTimes;

    private ItemSO currentItem;
    private SwordHitbox swordHitbox;
    private List<Collider> enemiesHitThisSwing;

    // Stores the player's rotation at the start of a melee attack to ensure the swing is not affected by mouse movement during the animation.
    private Quaternion initialAttackRotation;

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
                        break;
                    case ItemType.Ranged:
                        PerformRangedAttack();
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

        // Calculate the cooldown using the ItemSO's properties and set the end time for this specific slot.
        float finalCooldown = currentItem.attackCooldown / currentItem.fireRateMultiplier;
        slotCooldownEndTimes[activeSlot] = Time.time + finalCooldown;

        GameObject arrowGO = BulletPool.Instance.GetBullet();

        arrowGO.transform.position = arrowSpawnPoint.position;
        arrowGO.transform.rotation = arrowSpawnPoint.rotation;

        Arrow arrow = arrowGO.GetComponent<Arrow>();

        if (arrow != null)
        {
            arrow.Fire(currentItem.damage, this.arrowSpeed);
        }
    }

    private void PerformMeleeAttack()
    {
        int activeSlot = InventoryManager.Instance.activeSlotIndex;

        // Set the cooldown end time for this specific slot.
        float finalCooldown = currentItem.attackCooldown / currentItem.fireRateMultiplier;
        slotCooldownEndTimes[activeSlot] = Time.time + finalCooldown;

        enemiesHitThisSwing = new List<Collider>();
        initialAttackRotation = transform.rotation;
        StartCoroutine(AnimateMeleeSwing());
    }

    // Public method called by the SwordHitbox script when its trigger collides with something on the enemy layer.
    public void RegisterHit(Collider enemyCollider)
    {
        // Prevents hitting the same enemy multiple times with a single swing.
        if (enemiesHitThisSwing.Contains(enemyCollider)) return;

        enemiesHitThisSwing.Add(enemyCollider);

        float currentDamage = (currentItem != null) ? currentItem.damage : 10f;
        EnemyAI enemy = enemyCollider.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.TakeDamage((int)currentDamage);
        }

        //also chjeck if its a boss
        DashBoss boss = enemyCollider.GetComponent<DashBoss>();
        if (boss != null)
        {
            boss.TakeDamage((int)currentDamage);
        }
    }

    // Animates the visual swing of the melee weapon over the attackDuration.
    private IEnumerator AnimateMeleeSwing()
    {
        if (meleeAttackPivot == null || meleeWeaponVisual == null) yield break;

        meleeWeaponVisual.SetActive(true);

        float elapsedTime = 0f;
        bool swingLeftToRight = Random.value > 0.5f; // Randomize swing direction for variety.
        float startAngle = swingLeftToRight ? -attackAngle / 2 : attackAngle / 2;
        float endAngle = swingLeftToRight ? attackAngle / 2 : -attackAngle / 2;

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

        // Reset the pivot's rotation so it's aligned for the next attack.
        meleeAttackPivot.localRotation = Quaternion.identity;
        meleeWeaponVisual.SetActive(false);
    }
}