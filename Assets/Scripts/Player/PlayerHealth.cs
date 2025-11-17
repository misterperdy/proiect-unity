
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
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
        damage = Mathf.Max(0, damage);
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
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
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<PlayerAttack>().enabled = false;
        GetComponent<PlayerLookAtCursor>().enabled = false;
        
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
