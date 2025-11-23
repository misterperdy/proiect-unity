
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public bool canTakeDamage = true;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void Heal(int amount)
    {
        amount = Mathf.Max(0, amount);
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }


    public void TakeDamage(int damage)
    {
        if (canTakeDamage)
        {

            damage = Mathf.Max(0, damage);
            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                Die();
            }

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


        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        Invoke(nameof(ReloadLevel), 2f);
    }
    void ReloadLevel()
    {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
