
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public Slider slider;

    void Start()
    {
        if (playerHealth == null)
        {
            Debug.Log("PlayerHealth not assigned in inspector, trying to find it automatically.");
            playerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHealth>();
        }

        if (slider == null)
        {
            Debug.Log("Slider not assigned in inspector, trying to find it automatically.");
            slider = GetComponent<Slider>();
        }

        if (playerHealth != null && slider != null)
        {
            slider.maxValue = playerHealth.maxHealth;
            slider.value = playerHealth.currentHealth;
            Debug.Log("Health bar initialized successfully!");
        }
        else
        {
            Debug.LogError("Health bar initialization failed. Check for missing components or incorrect tags.");
        }
    }

    void Update()
    {
        if (slider != null && playerHealth != null)
        {
            slider.value = playerHealth.currentHealth;
        }
    }
}
