using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;
    public string mainMenuSceneName = "MainMenu";
    public string testScene = "TestScene";
    public static class GameOverState
    {
        public static string previousScene;
    }
    public void ShowGameOver()
    {
        // freeze game
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }

    public void Retry()
    {
        // unfreeze game
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameOverState.previousScene);

    }

    public void MainMenu()
    {
        // unfreeze and go to menu
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}