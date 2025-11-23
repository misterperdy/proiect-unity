using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject helpPanel;

    [Header("Scene To Load")]
    public string gameSceneName = "GameScene"; // The name of your actual game scene

    private void Start()
    {
        // Ensure we start with the main panel visible and help hidden
        ShowMain();
    }

    public void PlayGame()
    {
        // Load the game scene
        // Make sure to add the Game Scene to the Build Settings!
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game"); // Visible in Editor
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
