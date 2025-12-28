using UnityEngine;

public class XPOrb : MonoBehaviour
{
    public int xpAmount = 1;
    public float magnetRange = 5f;
    public float moveSpeed = 10f;

    private Transform player;
    private bool isMagnetized = false;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // 1. Check if player is close enough to pull the orb
        if (distance < magnetRange)
        {
            isMagnetized = true;
        }

        // 2. If magnetized, fly to player
        if (isMagnetized)
        {
            // Move towards player
            transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

            // Increase speed as it gets closer for snappy feel
            moveSpeed += 20f * Time.deltaTime;

            // 3. Absorption Logic
            if (distance < 0.5f)
            {
                LevelingSystem levels = player.GetComponent<LevelingSystem>();
                if (levels != null)
                {
                    levels.GainXP(xpAmount);
                }
                Destroy(gameObject);
            }
        }
    }

    // Called by the enemy when spawning this orb
    public void Initialize(int amount)
    {
        xpAmount = amount;
    }
}