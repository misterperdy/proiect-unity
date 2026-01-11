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

    void Awake()
    {
        // Create a 1x1 texture for the background
        backgroundTexture = new Texture2D(1, 1);
        backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f)); // Black with 70% opacity
        backgroundTexture.Apply();

        // Initialize styles
        titleStyle = new GUIStyle();
        titleStyle.fontSize = 48;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        subtitleStyle = new GUIStyle();
        subtitleStyle.fontSize = 24;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        if (isPaused)
        {
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
            GUI.Label(new Rect(menuRect.x, currentY, menuRect.width, 30), "Music Volume", subtitleStyle);
            currentY += 35;
            
            float currentMusicVol = 1f;
            if(MusicManager.Instance != null) currentMusicVol = MusicManager.Instance.musicVolume;
            
            float newMusicVol = GUI.HorizontalSlider(new Rect(menuRect.x + 50, currentY, menuRect.width - 100, 20), currentMusicVol, 0f, 1f);
            if(newMusicVol != currentMusicVol && MusicManager.Instance != null)
            {
                MusicManager.Instance.SetMusicVolume(newMusicVol);
            }
            currentY += 40;

            // SFX Volume
            GUI.Label(new Rect(menuRect.x, currentY, menuRect.width, 30), "SFX Volume", subtitleStyle);
            currentY += 35;

            float currentSFXVol = 1f;
            if (MusicManager.Instance != null) currentSFXVol = MusicManager.Instance.sfxVolume;

            float newSFXVol = GUI.HorizontalSlider(new Rect(menuRect.x + 50, currentY, menuRect.width - 100, 20), currentSFXVol, 0f, 1f);
            if (newSFXVol != currentSFXVol && MusicManager.Instance != null)
            {
                MusicManager.Instance.SetSFXVolume(newSFXVol);
            }
            // ----------------
        }
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
