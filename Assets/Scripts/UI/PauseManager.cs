using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static bool IsPaused = false;
    private bool isPaused {
        get { return IsPaused; }
        set { IsPaused = value; }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else if (Time.timeScale != 0f)
            {
                PauseGame();
            }
        }
    }

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private Texture2D backgroundTexture;

    private GUIStyle sliderTrackStyle;
    private GUIStyle sliderThumbStyle;
    private GUIStyle sliderLabelStyle;

    private bool guiStylesInitialized;

    private Texture2D sliderTrackTexture;
    private Texture2D sliderFillTexture;
    private Texture2D sliderThumbTexture;

    void Awake()
    {
        // Create a 1x1 texture for the background
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f)); // Black with 70% opacity
        backgroundTexture.Apply();

        sliderTrackTexture = new Texture2D(1, 1);
        sliderTrackTexture.SetPixel(0, 0, new Color(0.35f, 0.35f, 0.35f, 0.95f)); // gray track for visibility
        sliderTrackTexture.Apply();

        sliderFillTexture = new Texture2D(1, 1);
        sliderFillTexture.SetPixel(0, 0, new Color(0.85f, 0.85f, 0.85f, 0.95f)); // lighter fill
        sliderFillTexture.Apply();

        sliderThumbTexture = new Texture2D(1, 1);
        sliderThumbTexture.SetPixel(0, 0, new Color(1f, 1f, 1f, 1f));
        sliderThumbTexture.Apply();

        // NOTE: Do not touch GUI.skin (or other GUI calls) here.
        // GUI styles are initialized lazily inside OnGUI.
        guiStylesInitialized = false;
    }

    private void EnsureGuiStylesInitialized()
    {
        if (guiStylesInitialized) return;

        // Safe to reference GUI.skin only during OnGUI.
        titleStyle = new GUIStyle();
        titleStyle.fontSize = 48;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        subtitleStyle = new GUIStyle();
        subtitleStyle.fontSize = 24;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor = Color.white;

        sliderLabelStyle = new GUIStyle(subtitleStyle);
        sliderLabelStyle.alignment = TextAnchor.MiddleLeft;
        sliderLabelStyle.fontSize = 20;

        // We draw the track + fill ourselves for a cleaner look.
        sliderTrackStyle = new GUIStyle(GUI.skin.horizontalSlider);
        sliderTrackStyle.normal.background = null;
        sliderTrackStyle.active.background = null;
        sliderTrackStyle.hover.background = null;
        sliderTrackStyle.focused.background = null;
        sliderTrackStyle.fixedHeight = 18f;

        sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
        sliderThumbStyle.normal.background = sliderThumbTexture;
        sliderThumbStyle.active.background = sliderThumbTexture;
        sliderThumbStyle.hover.background = sliderThumbTexture;
        sliderThumbStyle.fixedWidth = 14f;
        sliderThumbStyle.fixedHeight = 22f;

        guiStylesInitialized = true;
    }

    void OnGUI()
    {
        if (isPaused)
        {
            EnsureGuiStylesInitialized();

            // Draw the semi-transparent background
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture);

            // Define the area for the pause menu text
            float menuWidth = 400;
            float menuHeight = 350; // Increased height for sliders
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            Rect menuRect = new Rect((screenWidth - menuWidth) / 2, (screenHeight - menuHeight) / 2, menuWidth, menuHeight);

            // Draw the title and subtitle
            float currentY = menuRect.y;
            GUI.Label(new Rect(menuRect.x, currentY, menuRect.width, 50), "Game Paused", titleStyle);
            currentY += 60;
            GUI.Label(new Rect(menuRect.x, currentY, menuRect.width, 30), "Press P to resume", subtitleStyle);
            currentY += 50;

            // --- Sliders ---
            // Music Volume
            float labelX = menuRect.x;
            float sliderX = menuRect.x + 50;
            float sliderW = menuRect.width - 100;

            GUI.Label(new Rect(labelX, currentY, menuRect.width, 30), "Music Volume", subtitleStyle);
            currentY += 35;
            
            float currentMusicVol = 1f;
            if(MusicManager.Instance != null) currentMusicVol = MusicManager.Instance.musicVolume;
            
            Rect musicSliderRect = new Rect(sliderX, currentY, sliderW, 20);
            DrawPrettySliderBackground(musicSliderRect, currentMusicVol);
            float newMusicVol = GUI.HorizontalSlider(musicSliderRect, currentMusicVol, 0f, 1f, sliderTrackStyle, sliderThumbStyle);
            if(newMusicVol != currentMusicVol && MusicManager.Instance != null)
            {
                MusicManager.Instance.SetMusicVolume(newMusicVol);
            }
            currentY += 40;

            // SFX Volume
            GUI.Label(new Rect(labelX, currentY, menuRect.width, 30), "SFX Volume", subtitleStyle);
            currentY += 35;

            float currentSFXVol = 1f;
            if (MusicManager.Instance != null) currentSFXVol = MusicManager.Instance.sfxVolume;

            Rect sfxSliderRect = new Rect(sliderX, currentY, sliderW, 20);
            DrawPrettySliderBackground(sfxSliderRect, currentSFXVol);
            float newSFXVol = GUI.HorizontalSlider(sfxSliderRect, currentSFXVol, 0f, 1f, sliderTrackStyle, sliderThumbStyle);
            if (newSFXVol != currentSFXVol && MusicManager.Instance != null)
            {
                MusicManager.Instance.SetSFXVolume(newSFXVol);
            }
            // ----------------
        }
    }

    private void DrawPrettySliderBackground(Rect rect, float value01)
    {
        // Track
        Rect track = new Rect(rect.x, rect.y + 2, rect.width, 16);
        GUI.DrawTexture(track, sliderTrackTexture);

        // Fill
        float clamped = Mathf.Clamp01(value01);
        Rect fill = new Rect(track.x, track.y, track.width * clamped, track.height);
        GUI.DrawTexture(fill, sliderFillTexture);
    }

    public void PauseGame()
    {   
        isPaused = true;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.gamePausedSfx);
        }

        Time.timeScale = 0f; // This freezes the game
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.gameUnpausedSfx);
        }

        Time.timeScale = 1f; // This resumes the game
        Debug.Log("Game Resumed");
    }
}
