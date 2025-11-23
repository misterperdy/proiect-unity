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
            else
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
            float menuHeight = 200;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            Rect menuRect = new Rect((screenWidth - menuWidth) / 2, (screenHeight - menuHeight) / 2, menuWidth, menuHeight);

            // Draw the title and subtitle
            GUI.Label(new Rect(menuRect.x, menuRect.y, menuRect.width, menuRect.height - 50), "Game Paused", titleStyle);
            GUI.Label(new Rect(menuRect.x, menuRect.y + 100, menuRect.width, menuRect.height - 150), "Press P to resume", subtitleStyle);
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // This freezes the game
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // This resumes the game
        Debug.Log("Game Resumed");
    }
}
