using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject helpPanel;

    [Header("Scene To Load")]
    public string gameSceneName = "Dungeon";

    [Header("Music Config")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;

    private void Awake()
    {
        InitializeMusic();
    }

    private void Start()
    {
        // Ensure we start with the main panel visible and help hidden
        ShowMain();
    }

    private void InitializeMusic()
    {
        // Try to find existing MusicManager
        if (MusicManager.Instance == null)
        {
            GameObject musicManagerObj = new GameObject("MusicManager");
            musicManagerObj.AddComponent<MusicManager>();
        }

        // Wait for next frame? No, simpler to just set clips.
        // If we just added component, Awake runs immediately. Instance should be set.
        
        if (MusicManager.Instance != null)
        {
             MusicManager.Instance.SetClips(mainMenuMusic, gameplayMusic, bossMusic);
             MusicManager.Instance.PlayMainMenuMusic();
        }
        else
        {
            Debug.LogError("MainMenuManager: Could not initialize MusicManager.");
        }
    }

    public void PlayGame()
    {
        // Switch to gameplay music right before loading, or let the game scene logic handle it?
        // User asked for "bgm_default_playing_game.mp3 for normal gameplay"
        // It's safer to play it when the game starts. 
        // We can do it here if "PlayGame" is the only entry point.
        MusicManager.Instance.PlayGameplayMusic();

        // Load the game scene
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game"); 
    }

    public void ShowHelp()
    {
        if(mainPanel != null) mainPanel.SetActive(false);
        if(helpPanel != null) helpPanel.SetActive(true);
    }

    public void ShowMain()
    {
        if(mainPanel != null) mainPanel.SetActive(true);
        if(helpPanel != null) helpPanel.SetActive(false);
    }
}
