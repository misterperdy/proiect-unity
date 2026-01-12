using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MainMenuStyler : MonoBehaviour
{
    [Header("Style Settings")]
    public Color primaryColor = new Color(0.1f, 0.1f, 0.2f, 1f); // Dark Blue
    public Color accentColor = new Color(0.2f, 0.6f, 1f, 1f); // Light Blue
    public Color textColor = Color.white;
    public Font customFont; // Optional: Assign in inspector if you have one

    [ContextMenu("Beautify UI")]
    public void BeautifyMenu()
    {
        MainMenuManager manager = GetComponent<MainMenuManager>();
        if (manager == null)
        {
            Debug.LogError("MainMenuManager component not found on this object!");
            return;
        }

        // style main panel
        if (manager.mainPanel != null)
        {
            StylePanel(manager.mainPanel, true);
            StyleButtons(manager.mainPanel);
        }
        else
        {
            Debug.LogWarning("Main Panel reference is missing in MainMenuManager!");
        }

        // style help panel
        if (manager.helpPanel != null)
        {
            StylePanel(manager.helpPanel, false);
            SetupHelpContent(manager.helpPanel);
        }
        else
        {
            Debug.LogWarning("Help Panel reference is missing in MainMenuManager!");
        }

        Debug.Log("UI Beautification Complete! Don't forget to save your scene.");
    }

    private void StylePanel(GameObject panel, bool isMain)
    {
        // add image for background if missing
        Image img = panel.GetComponent<Image>();
        if (img == null) img = panel.AddComponent<Image>();

        if (isMain)
        {
            // transparent for main menu
            img.color = new Color(0, 0, 0, 0);
        }
        else
        {
            // dark overlay for help
            img.color = new Color(0, 0, 0, 0.95f); // Slightly darker
        }

        // add layout group for spacing
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = panel.AddComponent<VerticalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = 40; // spacing so buttons dont overlap
        layout.padding = new RectOffset(40, 40, 40, 40);
    }

    private void StyleButtons(GameObject panel)
    {
        Button[] buttons = panel.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];

            // set button color
            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.color = primaryColor;
            }

            // add outline effect
            if (btn.GetComponent<Outline>() == null)
            {
                Outline outline = btn.gameObject.AddComponent<Outline>();
                outline.effectColor = accentColor;
                outline.effectDistance = new Vector2(2, -2);
            }

            // determine text content
            string textToSet = "BUTTON";
            string btnName = btn.name.ToLower();

            // guess by name
            if (btnName.Contains("play") || btnName.Contains("start")) textToSet = "PLAY GAME";
            else if (btnName.Contains("help") || btnName.Contains("control")) textToSet = "CONTROLS";
            else if (btnName.Contains("exit") || btnName.Contains("quit")) textToSet = "EXIT";
            // fallback to order if name is weird
            else
            {
                if (i == 0) textToSet = "PLAY GAME";
                else if (i == 1) textToSet = "CONTROLS";
                else if (i == 2) textToSet = "EXIT";
            }

            // set the text works for both legacy and textmeshpro
            SetButtonText(btn.gameObject, textToSet);

            // set fixed size
            LayoutElement le = btn.GetComponent<LayoutElement>();
            if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 200;
            le.minHeight = 50;
            le.preferredWidth = 300;
            le.preferredHeight = 60;

            // add animation script
            if (btn.GetComponent<UIButtonAnimation>() == null)
            {
                btn.gameObject.AddComponent<UIButtonAnimation>();
            }
        }
    }

    private void SetupHelpContent(GameObject panel)
    {
        string helpString = "WASD - Move\n" +
                            "Left Click - Attack\n" +
                            "Shift - Dash\n" +
                            "P - Pause\n" +
                            "1, 2 - Inventory\n" +
                            "F - Teleporter";

        // try to find existing text component
        bool textSet = SetTextOnObject(panel, helpString, 28);
        GameObject textObj = null;

        // if no text found look for child
        if (!textSet)
        {
            // check if we already created one
            Transform existingHelp = panel.transform.Find("HelpText");
            if (existingHelp != null)
            {
                textObj = existingHelp.gameObject;
                SetTextOnObject(textObj, helpString, 28);
            }
            else
            {
                textObj = new GameObject("HelpText");
                textObj.transform.SetParent(panel.transform);
                textObj.transform.localScale = Vector3.one;

                Text helpText = textObj.AddComponent<Text>();
                helpText.text = helpString;
                helpText.color = textColor;
                helpText.fontSize = 14;
                helpText.alignment = TextAnchor.MiddleCenter;
                if (customFont != null) helpText.font = customFont;
                else helpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                ContentSizeFitter csf = textObj.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained; // fixed height
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
        else
        {
            // usually we want a child object
        }

        // ensure text is top and correct height
        if (textObj != null)
        {
            textObj.transform.SetAsFirstSibling();

            RectTransform rt = textObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, 250);
            }

            // also update size fitter
            ContentSizeFitter csf = textObj.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        // style the back button
        Button backBtn = panel.GetComponentInChildren<Button>();
        if (backBtn != null)
        {
            SetButtonText(backBtn.gameObject, "BACK");

            // ensure button is bottom
            backBtn.transform.SetAsLastSibling();

            if (backBtn.GetComponent<UIButtonAnimation>() == null)
            {
                backBtn.gameObject.AddComponent<UIButtonAnimation>();
            }
        }
    }

    // helper to handle both legacy and tmp via reflection
    private void SetButtonText(GameObject btnObj, string content)
    {
        // try legacy text first
        Text legacyText = btnObj.GetComponentInChildren<Text>();
        if (legacyText != null)
        {
            legacyText.text = content;
            legacyText.color = textColor;
            legacyText.fontSize = 14;
            legacyText.alignment = TextAnchor.MiddleCenter;
            if (customFont != null) legacyText.font = customFont;
            return;
        }

        // try tmp via reflection to avoid errors
        Component[] components = btnObj.GetComponentsInChildren<Component>();
        foreach (Component c in components)
        {
            if (c == null) continue;
            System.Type type = c.GetType();
            if (type.Name == "TextMeshProUGUI" || type.Name == "TMP_Text")
            {
                try
                {
                    var prop = type.GetProperty("text");
                    if (prop != null) prop.SetValue(c, content);

                    var colorProp = type.GetProperty("color");
                    if (colorProp != null) colorProp.SetValue(c, textColor);

                    var alignProp = type.GetProperty("alignment");
                    // skipping alignment for tmp
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Found TMP but failed to set text: " + e.Message);
                }
                return;
            }
        }
    }

    private bool SetTextOnObject(GameObject obj, string content, int fontSize)
    {
        // check children
        Text legacyText = obj.GetComponentInChildren<Text>();
        if (legacyText != null)
        {
            legacyText.text = content;
            legacyText.fontSize = fontSize;
            legacyText.color = textColor;
            return true;
        }

        Component[] components = obj.GetComponentsInChildren<Component>();
        foreach (Component c in components)
        {
            if (c == null) continue;
            System.Type type = c.GetType();
            if (type.Name == "TextMeshProUGUI" || type.Name == "TMP_Text")
            {
                try
                {
                    var prop = type.GetProperty("text");
                    if (prop != null) prop.SetValue(c, content);

                    var colorProp = type.GetProperty("color");
                    if (colorProp != null) colorProp.SetValue(c, textColor);

                    var fontSizeProp = type.GetProperty("fontSize");
                    if (fontSizeProp != null) fontSizeProp.SetValue(c, (float)fontSize);

                    return true;
                }
                catch { }
            }
        }
        return false;
    }
}