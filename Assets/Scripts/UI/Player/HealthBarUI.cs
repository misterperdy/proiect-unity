
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public Slider slider;

    public GameObject HPtextGO;
    public float baseWidth = 230f;      // Latimea initiala (cand ai 100 HP)

    private float pixelsPerHP;

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
            pixelsPerHP = baseWidth / playerHealth.maxHealth;
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
        if (slider != null && playerHealth != null)
        {
            slider.value = playerHealth.currentHealth;
        }

        if(HPtextGO != null)
        {
            HPtextGO.GetComponent<Text>().text = playerHealth.currentHealth.ToString();
        }
    }

    public void UpdateWidth()
    {
        if (playerHealth != null)
        {
            slider.maxValue = playerHealth.maxHealth;
            float newWidth = playerHealth.maxHealth * pixelsPerHP;

            this.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth, this.gameObject.GetComponent<RectTransform>().sizeDelta.y);
        }
        
    }
}
