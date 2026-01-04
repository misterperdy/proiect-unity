using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject helpPanel;

    [Header("Scene To Load")]
    public string gameSceneName = "Dungeon"; 

    private void Start()
    {
        // Ensure we start with the main panel visible and help hidden
        ShowMain();
    }

    public void PlayGame()
    {
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
