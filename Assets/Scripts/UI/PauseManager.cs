using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static bool IsPaused = false;
    // property to access static var
    private bool isPaused
    {
        get { return IsPaused; }
        set { IsPaused = value; }
    }

    void Update()
    {
        // press P to pause
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            // check time scale so we dont pause if game is already stopped by something else
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
        // create textures manually with code (1 pixel textures)
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f)); // Black with transparency
        backgroundTexture.Apply();

        sliderTrackTexture = new Texture2D(1, 1);
        sliderTrackTexture.SetPixel(0, 0, new Color(0.35f, 0.35f, 0.35f, 0.95f)); // gray track
        sliderTrackTexture.Apply();

        sliderFillTexture = new Texture2D(1, 1);
        sliderFillTexture.SetPixel(0, 0, new Color(0.85f, 0.85f, 0.85f, 0.95f)); // lighter fill
        sliderFillTexture.Apply();

        sliderThumbTexture = new Texture2D(1, 1);
        sliderThumbTexture.SetPixel(0, 0, new Color(1f, 1f, 1f, 1f)); // white thumb
        sliderThumbTexture.Apply();

        // dont init styles here bc GUI.skin works only in OnGUI
        guiStylesInitialized = false;
    }

    private void EnsureGuiStylesInitialized()
    {
        if (guiStylesInitialized) return;

        // setup text styles
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

        // setup slider styles based on default skin
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

    // this runs every frame for UI
    void OnGUI()
    {
        if (isPaused)
        {
            EnsureGuiStylesInitialized();

            // draw big black background
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backgroundTexture);

            // math for centering the menu
            float menuWidth = 400;
            float menuHeight = 350;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            Rect menuRect = new Rect((screenWidth - menuWidth) / 2, (screenHeight - menuHeight) / 2, menuWidth, menuHeight);

            // draw texts
            float currentY = menuRect.y;
            GUI.Label(new Rect(menuRect.x, currentY, menuRect.width, 50), "Game Paused", titleStyle);
            currentY += 60;
            GUI.Label(new Rect(menuRect.x, currentY, menuRect.width, 30), "Press P to resume", subtitleStyle);
            currentY += 50;

            // --- Sliders Logic ---
            // Music Volume setup
            float labelX = menuRect.x;
            float sliderX = menuRect.x + 50;
            float sliderW = menuRect.width - 100;

            GUI.Label(new Rect(labelX, currentY, menuRect.width, 30), "Music Volume", subtitleStyle);
            currentY += 35;

            float currentMusicVol = 1f;
            if (MusicManager.Instance != null) currentMusicVol = MusicManager.Instance.musicVolume;

            Rect musicSliderRect = new Rect(sliderX, currentY, sliderW, 20);
            DrawPrettySliderBackground(musicSliderRect, currentMusicVol);
            float newMusicVol = GUI.HorizontalSlider(musicSliderRect, currentMusicVol, 0f, 1f, sliderTrackStyle, sliderThumbStyle);
            if (newMusicVol != currentMusicVol && MusicManager.Instance != null)
            {
                MusicManager.Instance.SetMusicVolume(newMusicVol);
            }
            currentY += 40;

            // SFX Volume setup
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
        // draw the track
        Rect track = new Rect(rect.x, rect.y + 2, rect.width, 16);
        GUI.DrawTexture(track, sliderTrackTexture);

        // draw the fill amount
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

        Time.timeScale = 0f; // freezes game logic
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySfx(MusicManager.Instance.gameUnpausedSfx);
        }

        Time.timeScale = 1f; // unfreezes game
        Debug.Log("Game Resumed");
    }
}