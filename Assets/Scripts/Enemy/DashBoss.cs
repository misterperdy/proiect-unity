using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DashBoss : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 200;
    public int currentHealth;
    public int contactDamage = 20; // contact damage to the player

    [Header("Detection")]
    public float sightRange = 30f; // how far to see you

    [Header("Dash Settings")]
    public float dashRange = 15f; // distance the attack starts from
    public float dashChargeTime = 1.5f; // Cue - how much he sits idle and aims at you
    public float dashSpeed = 40f;
    public float dashDuration = 0.5f;
    public float attackCooldown = 3f; // after a dash

    [Header("References")]
    public LineRenderer lineRenderer;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth; // reference to plyaerHealth scirpt
    private float defaultSpeed;
    private float defaultAcceleration;
    private bool isAttacking = false;

    private enum BossState { Idle, Chasing, ChargingDash, Dashing, Recovering }
    private BossState currentState;

    void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();

        //setup line renderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;

        //save normal speed
        defaultSpeed = agent.speed;
        defaultAcceleration = agent.acceleration;

        //find player
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerHealth = playerGO.GetComponent<PlayerHealth>();
        }

        currentState = BossState.Idle;
    }

    void Update()
    {
        if (isAttacking) return; // skip frame if he's attaciong

        //State Machine
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
                ChaseAndDecide();
                break;
        }
    }

    bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        //just check distance for now, no sight raycast check
        float distance = Vector3.Distance(transform.position, player.position);
        return distance < sightRange;
    }

    void ChaseAndDecide()
    {
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        //if we are in dash range and no t on cooldown, then dash
        if(distance <= dashRange)
        {
            StartCoroutine(PerformDashAttack());
        }
        else
        {
            //walk to him
            agent.SetDestination(player.position);
            agent.speed = defaultSpeed;
        }
    }

    //main attack logic
    private IEnumerator PerformDashAttack()
    {
        isAttacking = true;
        currentState = BossState.ChargingDash;

        //stop the agent
        agent.isStopped = true;
        agent.ResetPath(); // reset current path so it doesn't interfere

        // save position of player in the moment of locking in
        Vector3 lockPosition = player.position;

        //charge time + visual cue
        lineRenderer.enabled = true;
        float timer = 0f;

        while (timer < dashChargeTime)
        {
            timer += Time.deltaTime;

            transform.LookAt(new Vector3(lockPosition.x, transform.position.y, lockPosition.z));

            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, lockPosition);

            yield return null;
        }

        //execute dash
        lineRenderer.enabled = false;
        currentState = BossState.Dashing;

        //set huge speed on dash
        agent.speed = dashSpeed;
        agent.acceleration = 1000f;
        agent.isStopped = false;
        agent.SetDestination(lockPosition); // go to where he remembers the plyaer to be

        //let him arrive
        float dashTimer = 0f;
        while(dashTimer < dashDuration && agent.remainingDistance > 1f)
        {
            dashTimer += Time.deltaTime;

            CheckDamageCollision();

            yield return null;
        }

        //stop and recover
        agent.isStopped = true;
        agent.speed = defaultSpeed;
        agent.acceleration = defaultAcceleration;

        currentState = BossState.Recovering;

        // wait until to folow player again
        yield return new WaitForSeconds(attackCooldown);

        agent.isStopped = false;
        isAttacking = false;
        currentState = BossState.Chasing;
    }

    void CheckDamageCollision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 2f); // sphere around the boss
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamage);
                    //INVICIBILITY frames logic would go here, but now there is none.
                }
            }
        }
    }

    //for editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dashRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }
}
