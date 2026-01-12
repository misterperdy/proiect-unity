using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeBoss : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 1500;
    public int currentHealth;
    public int contactDamage = 15;

    [Header("Detection")]
    public float sightRange = 10f;

    [Header("Models")]
    public GameObject modelIdle; // Slime_1
    public GameObject modelJump; // Slime_4

    [Header("Small Jump Settings")]
    public float smallJumpDistance = 10f;
    public float smallJumpDuration = 0.8f;
    public float smallJumpHeight = 1.5f;
    public float timeBetweenJumps = 1f;

    [Header("Big Jump Settings")]
    public float bigJumpDuration = 2.0f;
    public float slamRadius = 4f;
    public float shadowFollowSpeed = 2f;

    [Header("References")]
    public GameObject shadowPrefab;
    public GameObject slimePuddlePrefab;

    private Transform player;
    private PlayerHealth playerHealth;
    private bool isAttacking = false;
    private Rigidbody rb;
    private float speedMultiplier = 1f;
    private GameObject activeShadow;

    [Header("visual adjustments")]
    public float jumpModelYOffset = 0.0f;
    public LayerMask floorLayer;

    [Header("Hit Effect")]
    public GameObject hitParticles;

    public enum BossState { Idle, Chasing, PreparingJump, Jumping, BigJumping, Recovering }
    public BossState currentState;

    [Header("UI")]
    public BossBarSlider bossHealthBar;

    private float lastDamageSfxTime = -999f;
    private const float damageSfxMinInterval = 0.08f;

    void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            // adding rigidbody if missing
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        SwapModel(false);

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHealth = playerGO.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogError("SlimeBoss: Could not find object with tag 'Player'!");
        }

        if (GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
        {
            Debug.LogWarning("SlimeBoss: Found a NavMeshAgent! Please remove it.");
        }

        currentState = BossState.Idle;

        if (bossHealthBar != null)
        {
            bossHealthBar.SetMaxHealth(maxHealth);
            bossHealthBar.ToggleBar(true);
        }
    }

    void Update()
    {
        // --- ROTATION LOGIC ---
        // constantly face the player
        if (player != null)
        {
            SmoothLookAt(player.position);
        }

        // blocking update if attacking
        if (isAttacking) return;

        switch (currentState)
        {
            case BossState.Idle:
                if (CanSeePlayer())
                {
                    if (MusicManager.Instance != null) MusicManager.Instance.PlayBossMusic();
                    currentState = BossState.Chasing;
                }
                break;
            case BossState.Chasing:
                ChaseAndDecide();
                break;
            case BossState.Recovering:
                break;
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        float distance = Vector3.Distance(transform.position, player.position);
        return distance < sightRange;
    }

    void ChaseAndDecide()
    {
        if (player == null) return;

        // rng to decide which jump to use
        float rng = Random.Range(0f, 1f);
        if (rng > 0.7f)
        {
            StartCoroutine(PerformBigJump());
        }
        else
        {
            StartCoroutine(PerformSmallJump());
        }
    }

    private IEnumerator PerformSmallJump()
    {
        isAttacking = true;
        currentState = BossState.PreparingJump;

        float realGroundY = transform.position.y;
        RaycastHit hit;
        // finding the ground level
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 5f, floorLayer))
        {
            realGroundY = hit.point.y;
        }

        // snap to ground
        transform.position = new Vector3(transform.position.x, realGroundY, transform.position.z);

        // 1. Prepare visual
        SwapModel(true);

        if (modelJump != null)
        {
            modelJump.transform.localPosition = new Vector3(0, jumpModelYOffset, 0);
        }

        // disable physics interfere
        if (rb != null) rb.isKinematic = true;

        yield return new WaitForSeconds(0.5f * speedMultiplier);

        currentState = BossState.Jumping;

        // 2. Calculate jump path
        Vector3 startPos = transform.position;
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 targetPos = transform.position + direction * smallJumpDistance;

        // keeping y level same
        float groundLevel = startPos.y;
        targetPos.y = groundLevel;

        float currentDuration = smallJumpDuration * speedMultiplier;
        float timer = 0f;

        while (timer < currentDuration)
        {
            timer += Time.deltaTime;
            float t = timer / currentDuration;

            // linear lerp for x/z
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // parabola math for the jump arc
            float heightOffset = 4 * smallJumpHeight * t * (1 - t);
            currentPos.y = groundLevel + heightOffset;

            transform.position = currentPos;
            yield return null;
        }

        // 3. Land
        // forcing exact position
        targetPos.y = groundLevel;
        transform.position = targetPos;

        SwapModel(false);
        CheckAreaDamage(2f); // dmg check on landing

        currentState = BossState.Recovering;
        yield return new WaitForSeconds(timeBetweenJumps * speedMultiplier);

        isAttacking = false;
        currentState = BossState.Chasing;
    }

    private IEnumerator PerformBigJump()
    {
        currentState = BossState.BigJumping;
        isAttacking = true;

        // 1. Setup ground
        float realGroundY = transform.position.y;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 10f, floorLayer))
        {
            realGroundY = hit.point.y;
        }

        // changing model
        SwapModel(true);
        if (modelJump != null) modelJump.transform.localPosition = new Vector3(0, jumpModelYOffset, 0);

        if (rb != null) rb.isKinematic = true;

        // 2. Flying up
        float ascentDuration = 0.5f;
        float airHeight = 15f; // fly high out of screen
        float timer = 0f;
        Vector3 startPos = transform.position;
        Vector3 highPos = new Vector3(startPos.x, realGroundY + airHeight, startPos.z);

        while (timer < ascentDuration)
        {
            timer += Time.deltaTime;
            float t = timer / ascentDuration;
            // smooth fly up
            transform.position = Vector3.Lerp(startPos, highPos, t * t);
            yield return null;
        }

        // 3. Spawning the shadow
        if (shadowPrefab != null)
        {
            // spawn shadow on floor
            Vector3 shadowPos = new Vector3(transform.position.x, realGroundY + 0.05f, transform.position.z);
            activeShadow = Instantiate(shadowPrefab, shadowPos, Quaternion.Euler(90, 0, 0));
        }

        // 4. Hovering and chasing
        float chaseDuration = bigJumpDuration * speedMultiplier;
        timer = 0f;

        while (timer < chaseDuration)
        {
            timer += Time.deltaTime;

            if (activeShadow != null && player != null)
            {
                // Move shadow towards player
                Vector3 dirToPlayer = (player.position - activeShadow.transform.position).normalized;
                dirToPlayer.y = 0;

                // moving shadow
                Vector3 newShadowPos = activeShadow.transform.position + dirToPlayer * shadowFollowSpeed * Time.deltaTime;

                // stick shadow to ground
                newShadowPos.y = realGroundY + 0.05f;

                activeShadow.transform.position = newShadowPos;

                // keep boss directly above shadow
                transform.position = new Vector3(newShadowPos.x, realGroundY + airHeight, newShadowPos.z);
            }

            yield return null;
        }

        // 5. Drop Down (Slam)
        Vector3 targetLandPos = transform.position;
        targetLandPos.y = realGroundY;

        float dropDuration = 0.2f; // fast fall
        timer = 0f;
        Vector3 currentHighPos = transform.position;

        while (timer < dropDuration)
        {
            timer += Time.deltaTime;
            float t = timer / dropDuration;
            transform.position = Vector3.Lerp(currentHighPos, targetLandPos, t);
            yield return null;
        }

        // snap to ground
        transform.position = targetLandPos;

        // dmg and spawn puddle
        CheckAreaDamage(slamRadius);
        if (slimePuddlePrefab != null) Instantiate(slimePuddlePrefab, transform.position, slimePuddlePrefab.transform.rotation);

        // 6. Cleanup
        if (activeShadow != null) Destroy(activeShadow);

        // reset model
        if (modelJump != null) modelJump.transform.localPosition = Vector3.zero;
        SwapModel(false);

        currentState = BossState.Recovering;
        yield return new WaitForSeconds(1f);

        isAttacking = false;
        currentState = BossState.Chasing;
    }

    void SwapModel(bool isJumping)
    {
        if (modelIdle) modelIdle.SetActive(!isJumping);
        if (modelJump) modelJump.SetActive(isJumping);
    }

    void SetVisualsVisible(bool state)
    {
        if (!state)
        {
            if (modelIdle) modelIdle.SetActive(false);
            if (modelJump) modelJump.SetActive(false);
        }
        else
        {
            if (modelIdle) modelIdle.SetActive(true);
            if (modelJump) modelJump.SetActive(false);
        }
    }

    void SmoothLookAt(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0; // look flat
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    void CheckAreaDamage(float radius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (playerHealth != null) playerHealth.TakeDamage(contactDamage);

                Rigidbody playerRb = hit.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 dir = (hit.transform.position - transform.position).normalized;
                    playerRb.AddForce(dir * 10f, ForceMode.Impulse);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (MusicManager.Instance != null && Time.time - lastDamageSfxTime >= damageSfxMinInterval)
        {
            MusicManager.Instance.PlaySpatialSfx(MusicManager.Instance.slimeBossTookDamageSfx, transform.position, 1f, 3f, 35f);
            lastDamageSfxTime = Time.time;
        }

        if (bossHealthBar != null) bossHealthBar.SetHealth(currentHealth);

        StartCoroutine(SetHitParticles());

        float healthPercent = (float)currentHealth / maxHealth;

        // speed up as health gets lower
        if (healthPercent <= 0.25f) speedMultiplier = 0.5f;
        else if (healthPercent <= 0.50f) speedMultiplier = 0.65f;
        else if (healthPercent <= 0.75f) speedMultiplier = 0.8f;


        if (currentHealth <= 0) Die();
    }

    private IEnumerator SetHitParticles()
    {
        hitParticles.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        hitParticles.SetActive(false);

    }

    void Die()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySpatialSfx(MusicManager.Instance.bossDiesSfx, transform.position, 1f, 3f, 45f);
        }

        if (MusicManager.Instance != null) MusicManager.Instance.PlayGameplayMusic();

        if (activeShadow != null) Destroy(activeShadow);
        if (bossHealthBar != null) bossHealthBar.ToggleBar(false);
        Destroy(gameObject);

    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerHealth != null) playerHealth.TakeDamage(contactDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}