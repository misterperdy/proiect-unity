using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public Slider slider;

    public GameObject HPtextGO;
    public float baseWidth = 230f;      // default width at start

    private float pixelsPerHP;

    void Start()
    {
        // try finding player health if null
        if (playerHealth == null)
        {
            Debug.Log("PlayerHealth not assigned in inspector, trying to find it automatically.");
            playerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHealth>();
        }

        // try finding slider
        if (slider == null)
        {
            Debug.Log("Slider not assigned in inspector, trying to find it automatically.");
            slider = GetComponent<Slider>();
        }

        // setup logic
        if (playerHealth != null && slider != null)
        {
            slider.maxValue = playerHealth.maxHealth;
            slider.value = playerHealth.currentHealth;
            pixelsPerHP = baseWidth / playerHealth.maxHealth; // calc pixels per 1 hp
            Debug.Log("Health bar initialized successfully!");
        }
        else
        {
            Debug.LogError("Health bar initialization failed. Check for missing components or incorrect tags.");
        }

        UpdateWidth();
    }

    void Update()
    {
        // update slider value constantly
        if (slider != null && playerHealth != null)
        {
            slider.value = playerHealth.currentHealth;
        }

        // update text number
        if (HPtextGO != null)
        {
            HPtextGO.GetComponent<Text>().text = playerHealth.currentHealth.ToString();
        }
    }

    // method to change width if we get max hp upgrade
    public void UpdateWidth()
    {
        if (playerHealth != null)
        {
            slider.maxValue = playerHealth.maxHealth;
            float newWidth = playerHealth.maxHealth * pixelsPerHP;

            // set rect transform size
            this.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth, this.gameObject.GetComponent<RectTransform>().sizeDelta.y);
        }

    }
}