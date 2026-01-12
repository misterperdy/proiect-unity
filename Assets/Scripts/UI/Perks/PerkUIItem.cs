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

    // setup called by manager
    public void Setup(PerkSO perk, PerkManager mgr)
    {
        myPerk = perk;
        manager = mgr;

        nameText.text = perk.perkName;
        descText.text = perk.description;
        if (perk.icon != null) iconImage.sprite = perk.icon;

        // reset listeners just in case
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        // play sound
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.perkSelectedSfx);
        }
        // tell manager we picked this
        manager.SelectPerk(myPerk);
    }
}