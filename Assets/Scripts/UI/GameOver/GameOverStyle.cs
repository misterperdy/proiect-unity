using UnityEngine;
using UnityEngine.UI;

public class GameOverStyler : MonoBehaviour
{
    [Header("Style Settings")]
    public Color primaryColor = new Color(0.1f, 0.1f, 0.2f, 1f);
    public Color accentColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color textColor = Color.white;
    public Font customFont;

    void Start()
    {
        BeautifyUI();
    }

    [ContextMenu("Beautify GameOver UI")]
    public void BeautifyUI()
    {
        Button[] buttons = GetComponentsInChildren<Button>();
        if (buttons.Length == 0)
        {
            Debug.LogWarning("No buttons found under GameOver panel!");
            return;
        }

        foreach (Button btn in buttons)
        {
            StyleButton(btn);
        }
    }

    private void StyleButton(Button btn)
    {
        Image btnImg = btn.GetComponent<Image>();
        if (btnImg != null)
            btnImg.color = primaryColor;

        if (btn.GetComponent<Outline>() == null)
        {
            Outline outline = btn.gameObject.AddComponent<Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, -2);
        }

        SetButtonText(btn);

        LayoutElement le = btn.GetComponent<LayoutElement>();
        if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
        le.minWidth = 200;
        le.minHeight = 50;
        le.preferredWidth = 300;
        le.preferredHeight = 60;

        if (btn.GetComponent<UIButtonAnimation>() == null)
            btn.gameObject.AddComponent<UIButtonAnimation>();
    }

    private void SetButtonText(Button btn)
    {
        Text txt = btn.GetComponentInChildren<Text>();
        if (txt != null)
        {

            string lowerName = btn.name.ToLower();
            if (lowerName.Contains("retry") || lowerName.Contains("play"))
                txt.text = "RETRY";
            else if (lowerName.Contains("menu") || lowerName.Contains("main"))
                txt.text = "MAIN MENU";
            else
                txt.text = "BUTTON";

            txt.color = textColor;
            txt.fontSize = 14;
            txt.alignment = TextAnchor.MiddleCenter;
            if (customFont != null) txt.font = customFont;
        }
    }
}