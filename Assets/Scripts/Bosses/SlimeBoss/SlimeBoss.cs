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

    public enum BossState { Idle, Chasing, PreparingJump, Jumping, BigJumping, Recovering }
    public BossState currentState;

    [Header("UI")]
    public BossBarSlider bossHealthBar;

    void Start()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
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
        // Always look at the player, regardless of state or if attacking
        if (player != null)
        {
            SmoothLookAt(player.position);
        }

        // Stop state machine logic if we are in the middle of an attack coroutine
        if (isAttacking) return;

        switch (currentState)
        {
            case BossState.Idle:
                if (CanSeePlayer())
                {
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

        // Note: SmoothLookAt is handled in Update now

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

        // 1. Prepare
        SwapModel(true);
        // Ensure physics doesn't mess with our manual movement
        if (rb != null) rb.isKinematic = true;

        yield return new WaitForSeconds(0.5f * speedMultiplier);

        currentState = BossState.Jumping;

        // 2. Calculate Targets
        Vector3 startPos = transform.position;
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 targetPos = transform.position + direction * smallJumpDistance;

        // Lock the Y height to the starting height so we don't drift underground
        float groundLevel = startPos.y;
        targetPos.y = groundLevel;

        float currentDuration = smallJumpDuration * speedMultiplier;
        float timer = 0f;

        while (timer < currentDuration)
        {
            timer += Time.deltaTime;
            float t = timer / currentDuration;

            // Move X/Z linearly
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // Parabola: 4 * height * t * (1-t)
            float heightOffset = 4 * smallJumpHeight * t * (1 - t);
            currentPos.y = groundLevel + heightOffset;

            transform.position = currentPos;
            yield return null;
        }

        // 3. Land
        // Force exact position to fix any floating point errors
        targetPos.y = groundLevel;
        transform.position = targetPos;

        SwapModel(false);
        CheckAreaDamage(2f);

        // Optional: Turn physics back on if you want gravity to work while chasing
        // if(rb != null) rb.isKinematic = false; 

        currentState = BossState.Recovering;
        yield return new WaitForSeconds(timeBetweenJumps * speedMultiplier);

        isAttacking = false;
        currentState = BossState.Chasing;
    }

    private IEnumerator PerformBigJump()
    {
        isAttacking = true;
        currentState = BossState.PreparingJump;

        SwapModel(true);
        yield return new WaitForSeconds(0.5f * speedMultiplier);

        currentState = BossState.BigJumping;

        SetVisualsVisible(false);

        if (shadowPrefab != null)
        {
            activeShadow = Instantiate(shadowPrefab, player.position, Quaternion.Euler(90, 0, 0));
        }

        float currentAirDuration = bigJumpDuration * speedMultiplier;
        float flashTime = 0.35f; //ms before slam indicator turns red
        float flashTriggerTime = currentAirDuration - flashTime;
        bool hasFlashed = false;


        float airTimer = 0f;
        while (airTimer < currentAirDuration)
        {
            airTimer += Time.deltaTime;

            // 1. Move Shadow
            if (activeShadow != null && player != null)
            {
                Vector3 targetShadowPos = player.position;
                targetShadowPos.y = 0.212f;
                float currentFollowSpeed = shadowFollowSpeed * (1f / speedMultiplier);
                activeShadow.transform.position = Vector3.Lerp(activeShadow.transform.position, targetShadowPos, Time.deltaTime * currentFollowSpeed);

                // 2. Check for Flash Time
                if (!hasFlashed && airTimer >= flashTriggerTime)
                {
                    hasFlashed = true;

                    // Try to get SpriteRenderer (2D way)
                    SpriteRenderer sr = activeShadow.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = Color.red;
                    }
                    else
                    {
                        // Fallback to MeshRenderer (3D Quad way)
                        Renderer r = activeShadow.GetComponent<Renderer>();
                        if (r != null) r.material.color = Color.red;
                    }
                }
            }
            yield return null;
        }

        // Slam
        Vector3 landingSpot = transform.position;
        if (activeShadow != null)
        {
            landingSpot = activeShadow.transform.position;
            landingSpot.y = transform.position.y;
            Destroy(activeShadow);
            activeShadow = null;
        }

        transform.position = landingSpot;
        if (player != null) transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        SetVisualsVisible(true);
        SwapModel(false);

        if (slimePuddlePrefab != null)
        {
            Vector3 puddlePos = landingSpot;
            puddlePos.y = 0.212f;
            Instantiate(slimePuddlePrefab, puddlePos, Quaternion.Euler(90, 0, 0));
        }

        CheckAreaDamage(slamRadius);

        currentState = BossState.Recovering;
        yield return new WaitForSeconds((timeBetweenJumps + 1f) * speedMultiplier);

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
        direction.y = 0; // Keep looking flat, don't look up/down
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
        if (bossHealthBar != null) bossHealthBar.SetHealth(currentHealth);

        float healthPercent = (float)currentHealth / maxHealth;

        if (healthPercent <= 0.25f) speedMultiplier = 0.5f; 
        else if (healthPercent <= 0.50f) speedMultiplier = 0.65f; 
        else if (healthPercent <= 0.75f) speedMultiplier = 0.8f; 
        
        
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
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