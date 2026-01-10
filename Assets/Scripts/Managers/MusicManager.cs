using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Ensure a MusicManager exists as early as possible so menu audio works immediately.
        if (Instance != null)
        {
            EnsureAudioListenerExists();
            return;
        }

        MusicManager existing = Object.FindObjectOfType<MusicManager>();
        if (existing != null)
        {
            Instance = existing;
            Object.DontDestroyOnLoad(existing.gameObject);
            EnsureAudioListenerExists();
            return;
        }

        GameObject obj = new GameObject("MusicManager");
        obj.AddComponent<MusicManager>();
    }

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;
    
    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume = 0.1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioListenerExists();
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.loop = true;
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // force 2D
            }

            audioSource.volume = musicVolume;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start() 
    {
        // Periodic check to enforce volume on new SFX
        StartCoroutine(EnforceSFXVolumeRoutine());
    }

    public void PlayMainMenuMusic()
    {
        if (mainMenuMusic == null)
        {
            Debug.LogWarning("MusicManager: mainMenuMusic is not assigned (nothing to play). Assign bgm_main_menu.mp3 in MainMenuManager.");
            return;
        }
        PlayMusic(mainMenuMusic);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    public void PlayBossMusic()
    {
        PlayMusic(bossMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        
        // Ensure volume is set for music
        audioSource.volume = musicVolume;

        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.time = 0f;
        audioSource.Play();
    }

    private static void EnsureAudioListenerExists()
    {
        // If the main menu scene has no AudioListener, audio will be inaudible.
        if (Object.FindObjectOfType<AudioListener>() != null) return;

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.gameObject.AddComponent<AudioListener>();
            return;
        }

        GameObject listenerObj = new GameObject("AudioListener");
        listenerObj.AddComponent<AudioListener>();
        Object.DontDestroyOnLoad(listenerObj);
    }
    
    // Helper to set clips if passed from another manager
    public void SetClips(AudioClip menu, AudioClip game, AudioClip boss)
    {
        if(menu != null) mainMenuMusic = menu;
        if(game != null) gameplayMusic = game;
        if(boss != null) bossMusic = boss;
    }

    public void SetMusicVolume(float vol)
    {
        musicVolume = vol;
        if (audioSource != null) audioSource.volume = musicVolume;
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = vol;
        UpdateAllSFXVolume();
    }

    private void UpdateAllSFXVolume()
    {
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allSources)
        {
            // Skip the background music source
            if (source == audioSource) continue;
            source.volume = sfxVolume;
        }
    }

    private IEnumerator EnforceSFXVolumeRoutine()
    {
        while (true)
        {
            UpdateAllSFXVolume();
            yield return new WaitForSeconds(1.0f); // Check every second for new spawned objects
        }
    }
}
