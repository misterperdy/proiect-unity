using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameOverManager;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Invincibility Frames")]
    public bool canTakeDamage = true;
    public float invincibilityDuration = 2f;
    public float flashInterval = 0.15f;

    public bool IsHurt { get; private set; }

    private bool isDead = false;
    private Renderer[] modelRenderers;
    public GameOverManager gameOverManager;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }
    void Start()
    {
        currentHealth = maxHealth;
        modelRenderers = GetComponentsInChildren<Renderer>();
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        amount = Mathf.Max(0, amount);
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void HealPercent(float percent)
    {
        if (isDead) return;
        float newHp = currentHealth + (percent * maxHealth);
        currentHealth = (int)Mathf.Min(newHp, maxHealth);

    }


    public void TakeDamage(int damage)
    {
        if (canTakeDamage && !isDead)
        {
            damage = Mathf.Max(0, damage);
            currentHealth -= damage;
            IsHurt = true;
            animator.SetTrigger("t_damage");

            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlaySfx(MusicManager.Instance.playerTookDamageSfx);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(HurtRecoveryRoutine());
                StartCoroutine(InvincibilityRoutine());
            }
        }
    }

    // coroutine for iframes
    private IEnumerator InvincibilityRoutine()
    {
        // player invincible now
        canTakeDamage = false;

        // blink effect
        float timer = 0f;
        while (timer < invincibilityDuration)
        {
            // turn off renderers
            ToggleRenderers(false);
            yield return new WaitForSeconds(flashInterval);
            ToggleRenderers(true);
            yield return new WaitForSeconds(flashInterval);

            timer += flashInterval * 2;
        }

        ToggleRenderers(true); // make sure visible at end
        canTakeDamage = true; // damage enabled
    }

    void ToggleRenderers(bool State)
    {
        foreach (Renderer r in modelRenderers)
        {
            r.enabled = State;
        }
    }

    public void Update()
    {

        if (transform.position.y < -50f)
        {
            currentHealth = 0;
            Die();
        }
    }


    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Player has died!");

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.playerDiesGameOverSfx);
        }

        animator.SetTrigger("t_dead");

        StopAllCoroutines();
        ToggleRenderers(true);

        MonoBehaviour[] allScripts = GetComponentsInChildren<MonoBehaviour>();

        foreach (MonoBehaviour script in allScripts)
        {
            if (script != this)
            {
                script.enabled = false;
            }
        }

        TeleporterBoss teleporter = GetComponent<TeleporterBoss>();
        if (teleporter != null)
            teleporter.enabled = false;

        Medkit medkit = GetComponent<Medkit>();
        if (medkit != null)
            medkit.enabled = false;


        //Rigidbody rb = GetComponent<Rigidbody>();
        //if (rb != null)
        //    rb.isKinematic = false;

        Invoke(nameof(LoadGameOverScene), 1.5f);

    }
    void LoadGameOverScene()
    {
        GameOverState.previousScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene("GameOver");
    }

    private IEnumerator HurtRecoveryRoutine()
    {
        yield return new WaitForSeconds(0.6f);
        IsHurt = false;
    }
}