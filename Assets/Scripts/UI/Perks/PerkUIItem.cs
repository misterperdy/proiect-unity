using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerkUIItem : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Image iconImage;
    public Button button;

    private PerkSO myPerk;
    private PerkManager manager;

    public void Setup(PerkSO perk, PerkManager mgr)
    {
        myPerk = perk;
        manager = mgr;

        nameText.text = perk.perkName;
        descText.text = perk.description;
        if (perk.icon != null) iconImage.sprite = perk.icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.perkSelectedSfx);
        }
        manager.SelectPerk(myPerk);
    }
}