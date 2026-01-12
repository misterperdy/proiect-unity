using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject helpPanel;
    public GameObject volumePanel;

    [Header("Scene To Load")]
    public string gameSceneName = "Dungeon";

    [Header("Music Config")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;

    [Header("UI SFX")]
    public AudioClip uiHoverSfx;
    public AudioClip uiClickSfx;

    [Header("Gameplay SFX")]
    public AudioClip playerWalkingSfx;
    public AudioClip playerRunningSfx;
    public AudioClip playerMeleeSwingSfx;

    [Header("Teleporter SFX")]
    public AudioClip normalTeleporterSfx;
    public AudioClip bossTeleporterSfx;

    [Header("Combat SFX")]
    public AudioClip playerBowShootSfx;
    public AudioClip playerStaffUseSfx;
    public AudioClip enemyBowShootSfx;
    public AudioClip enemySwordSwingSfx;

    [Header("Damage SFX")]
    public AudioClip playerTookDamageSfx;
    public AudioClip skeletonTookDamageSfx;
    public AudioClip slimeEnemyTookDamageSfx;
    public AudioClip slimeBossTookDamageSfx;
    public AudioClip leechBossTookDamageSfx;
    public AudioClip golemBossTookDamageSfx;

    [Header("Perk SFX")]
    public AudioClip perkSelectedSfx;

    [Header("Other SFX")]
    public AudioClip playerDashSfx;
    public AudioClip enemyDiesSfx;
    public AudioClip bossDiesSfx;
    public AudioClip playerDiesGameOverSfx;
    public AudioClip gamePausedSfx;
    public AudioClip gameUnpausedSfx;
    public AudioClip xpPickupSfx;
    public AudioClip weaponPickupSfx;
    public AudioClip playerMedkitHealingSfx;
    public AudioClip turretShootSfx;

    private void Awake()
    {
        InitializeMusic();
    }

    private void Start()
    {
        // ensure start with main panel visible
        ShowMain();
    }

    private void InitializeMusic()
    {
        // try finding existing music manager
        if (MusicManager.Instance == null)
        {
            GameObject musicManagerObj = new GameObject("MusicManager");
            musicManagerObj.AddComponent<MusicManager>();
        }

        // wait for next frame no need simple set clips
        // awake runs immediately so instance should be set

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetClips(mainMenuMusic, gameplayMusic, bossMusic);
            MusicManager.Instance.SetUISfxClips(uiHoverSfx, uiClickSfx);
            MusicManager.Instance.SetGameplaySfxClips(playerWalkingSfx, playerRunningSfx, playerMeleeSwingSfx);
            MusicManager.Instance.SetTeleporterSfxClips(normalTeleporterSfx, bossTeleporterSfx);

            MusicManager.Instance.SetPlayerCombatSfxClips(playerBowShootSfx, playerStaffUseSfx, playerTookDamageSfx);
            MusicManager.Instance.SetEnemyCombatSfxClips(enemyBowShootSfx, enemySwordSwingSfx);
            MusicManager.Instance.SetEnemyDamageSfxClips(skeletonTookDamageSfx, slimeEnemyTookDamageSfx);
            MusicManager.Instance.SetBossDamageSfxClips(slimeBossTookDamageSfx, leechBossTookDamageSfx, golemBossTookDamageSfx);
            MusicManager.Instance.SetPerkSfxClips(perkSelectedSfx);

            MusicManager.Instance.SetAdditionalSfxClips(
                playerDashSfx,
                enemyDiesSfx,
                bossDiesSfx,
                playerDiesGameOverSfx,
                gamePausedSfx,
                gameUnpausedSfx,
                xpPickupSfx,
                weaponPickupSfx,
                playerMedkitHealingSfx,
                turretShootSfx);

            MusicManager.Instance.PlayMainMenuMusic();
        }
        else
        {
            Debug.LogError("MainMenuManager: Could not initialize MusicManager.");
        }
    }

    public void PlayGame()
    {
        // switch music to gameplay right before loading
        // user asked for default music for normal gameplay
        MusicManager.Instance.PlayGameplayMusic();

        // load scene
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    public void ShowHelp()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (volumePanel != null) volumePanel.SetActive(false);
        if (helpPanel != null) helpPanel.SetActive(true);
    }

    public void ShowMain()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (helpPanel != null) helpPanel.SetActive(false);
        if (volumePanel != null) volumePanel.SetActive(true);
    }
}