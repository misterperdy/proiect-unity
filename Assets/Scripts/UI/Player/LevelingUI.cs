using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Needed for the Slider
using TMPro;          // Needed for the Text

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

        // 1. Update the Slider (XP Bar)
        // We cast to float because int division returns 0 (e.g. 5/10 = 0)
        float progress = (float)levelingSystem.currentXP / levelingSystem.xpRequiredForNextLevel;

        // Use Mathf.Lerp for a smooth visual effect (optional but nice)
        xpSlider.value = Mathf.Lerp(xpSlider.value, progress, Time.deltaTime * 5f);

        // 2. Update the Level Text
        levelText.text = "Lvl " + levelingSystem.currentLevel.ToString();

        if(levelingSystem.currentAmountGained != 0)
        {   
            xpText.text = "+" + levelingSystem.currentAmountGained.ToString(); 
            StartCoroutine(FadeText(0.5f,xpText));
            levelingSystem.currentAmountGained = 0;
        }
    }

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