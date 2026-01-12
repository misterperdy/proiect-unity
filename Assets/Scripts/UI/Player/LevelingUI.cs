using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // for slider
using TMPro;          // for text mesh pro

public class LevelingUI : MonoBehaviour
{
    [Header("References")]
    public LevelingSystem levelingSystem;
    public Slider xpSlider;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI xpText;

    void Update()
    {
        if (levelingSystem == null) return;

        // 1. calculate slider fill amount
        // cast float important or it returns 0
        float progress = (float)levelingSystem.currentXP / levelingSystem.xpRequiredForNextLevel;

        // use lerp so it moves smooth not instant
        xpSlider.value = Mathf.Lerp(xpSlider.value, progress, Time.deltaTime * 5f);

        // 2. set text level
        levelText.text = "Lvl " + levelingSystem.currentLevel.ToString();

        // show floating xp gain text
        if (levelingSystem.currentAmountGained != 0)
        {
            xpText.text = "+" + levelingSystem.currentAmountGained.ToString();
            StartCoroutine(FadeText(0.5f, xpText)); // start fading
            levelingSystem.currentAmountGained = 0; // reset
        }
    }

    // coroutine to fade out text alpha
    public IEnumerator FadeText(float t, TextMeshProUGUI i)
    {
        i.color = new Color(i.color.r, i.color.g, i.color.b, 1);
        while (i.color.a > 0.0f)
        {
            i.color = new Color(i.color.r, i.color.g, i.color.b, i.color.a - (Time.deltaTime / t));
            yield return null;
        }
    }
}