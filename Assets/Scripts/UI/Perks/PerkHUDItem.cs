using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerkHUDItem : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI countText;

    public void Setup(PerkSO perk, int count)
    {
        iconImage.sprite = perk.icon;

        if (count > 1)
        {
            countText.gameObject.SetActive(true);
            countText.text = "x" + count.ToString();
        }
        else
        {
            countText.gameObject.SetActive(false); // Hide text if we only have 1
        }
    }
}