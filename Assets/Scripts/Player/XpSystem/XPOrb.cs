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

        // 1. check if close enough to start flying
        if (distance < magnetRange)
        {
            isMagnetized = true;
        }

        // 2. moving logic
        if (isMagnetized)
        {
            // fly to player
            transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

            // speed up over time so it looks snappy
            moveSpeed += 20f * Time.deltaTime;

            // 3. collect logic when close
            if (distance < 0.5f)
            {
                LevelingSystem levels = player.GetComponent<LevelingSystem>();
                if (levels != null)
                {
                    levels.GainXP(xpAmount);

                    if (MusicManager.Instance != null)
                    {
                        MusicManager.Instance.PlaySfx(MusicManager.Instance.xpPickupSfx);
                    }
                }
                Destroy(gameObject); // consume orb
            }
        }
    }

    // function to set xp amount when spawned
    public void Initialize(int amount)
    {
        xpAmount = amount;
    }
}