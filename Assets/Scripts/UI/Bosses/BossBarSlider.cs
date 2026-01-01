using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossBarSlider : MonoBehaviour
{
    public Slider slider;
    public GameObject canvas;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void SetMaxHealth(int maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
    }

    public void SetHealth(int currentHealth)
    {
        slider.value = currentHealth;
    }

    public void ToggleBar(bool state)
    {
        canvas.SetActive(state);
    }
}
