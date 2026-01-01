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

        // 1. Style Main Panel
        if (manager.mainPanel != null)
        {
            StylePanel(manager.mainPanel, true);
            StyleButtons(manager.mainPanel);
        }
        else
        {
            Debug.LogWarning("Main Panel reference is missing in MainMenuManager!");
        }

        // 2. Style Help Panel
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
        // Add Image if missing for background color
        Image img = panel.GetComponent<Image>();
        if (img == null) img = panel.AddComponent<Image>();
        
        if (isMain)
        {
            // Transparent or very subtle for main menu buttons container
            img.color = new Color(0, 0, 0, 0); 
        }
        else
        {
            // Dark overlay for help
            img.color = new Color(0, 0, 0, 0.95f); // Slightly darker
        }

        // Add Vertical Layout Group
        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = panel.AddComponent<VerticalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = 40; // Increased spacing to prevent overlap
        layout.padding = new RectOffset(40, 40, 40, 40);
    }

    private void StyleButtons(GameObject panel)
    {
        Button[] buttons = panel.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            
            // Style the Button Image
            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.color = primaryColor;
            }

            // Add Outline/Shadow for depth
            if (btn.GetComponent<Outline>() == null)
            {
                Outline outline = btn.gameObject.AddComponent<Outline>();
                outline.effectColor = accentColor;
                outline.effectDistance = new Vector2(2, -2);
            }

            // Determine Text Content
            string textToSet = "BUTTON";
            string btnName = btn.name.ToLower();
            
            // 1. Try to guess by name
            if (btnName.Contains("play") || btnName.Contains("start")) textToSet = "PLAY GAME";
            else if (btnName.Contains("help") || btnName.Contains("control")) textToSet = "CONTROLS";
            else if (btnName.Contains("exit") || btnName.Contains("quit")) textToSet = "EXIT";
            // 2. Fallback to order if name is generic (e.g. "Button")
            else
            {
                if (i == 0) textToSet = "PLAY GAME";
                else if (i == 1) textToSet = "CONTROLS";
                else if (i == 2) textToSet = "EXIT";
            }

            // Set the text (Handles both Legacy Text and TextMeshPro)
            SetButtonText(btn.gameObject, textToSet);

            // Set Size
            LayoutElement le = btn.GetComponent<LayoutElement>();
            if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 200;
            le.minHeight = 50;
            le.preferredWidth = 300;
            le.preferredHeight = 60;

            // Add Animation Script
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

        // Try to find existing text component (Legacy or TMP)
        bool textSet = SetTextOnObject(panel, helpString, 28);
        GameObject textObj = null;

        // If no text component found on panel itself, look for child or create one
        if (!textSet)
        {
            // Check if we already created one
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
                csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained; // User wants fixed height
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
        else
        {
            // If the panel itself has text, that's weird for a layout group parent, but okay.
            // Usually we want a child object.
        }

        // Ensure Text is at the top and has correct height
        if (textObj != null)
        {
            textObj.transform.SetAsFirstSibling();
            
            RectTransform rt = textObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, 250);
            }
            
            // Also update ContentSizeFitter if it exists on an existing object
            ContentSizeFitter csf = textObj.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        // Style the Back Button
        Button backBtn = panel.GetComponentInChildren<Button>();
        if (backBtn != null)
        {
            SetButtonText(backBtn.gameObject, "BACK");
            
            // Ensure Button is at the bottom
            backBtn.transform.SetAsLastSibling();
            
            if (backBtn.GetComponent<UIButtonAnimation>() == null)
            {
                backBtn.gameObject.AddComponent<UIButtonAnimation>();
            }
        }
    }

    // Helper to handle both Legacy Text and TextMeshPro via Reflection
    private void SetButtonText(GameObject btnObj, string content)
    {
        // 1. Try Legacy Text
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

        // 2. Try TextMeshPro (Reflection to avoid missing assembly error)
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
                    // TMP alignment is an enum, hard to set via reflection without knowing the enum value. 
                    // Skipping alignment for TMP to avoid errors, usually defaults to center or can be set in prefab.
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
        // Check children too
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
