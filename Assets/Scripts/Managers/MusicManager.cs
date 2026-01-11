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

    [Header("SFX Source")]
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip bossMusic;

    [Header("UI SFX")]
    public AudioClip uiHoverSfx;
    public AudioClip uiClickSfx;

    [Header("Gameplay SFX")]
    public AudioClip playerWalkingSfx;
    public AudioClip playerMeleeSwingSfx;
    public AudioClip playerBowShootSfx;
    public AudioClip playerStaffUseSfx;
    public AudioClip playerTookDamageSfx;

    public AudioClip playerDashSfx;
    public AudioClip playerDiesGameOverSfx;

    public AudioClip enemyBowShootSfx;
    public AudioClip enemySwordSwingSfx;

    public AudioClip perkSelectedSfx;

    public AudioClip xpPickupSfx;
    public AudioClip weaponPickupSfx;
    public AudioClip playerMedkitHealingSfx;

    public AudioClip skeletonTookDamageSfx;
    public AudioClip slimeEnemyTookDamageSfx;
    public AudioClip slimeBossTookDamageSfx;
    public AudioClip leechBossTookDamageSfx;
    public AudioClip golemBossTookDamageSfx;

    public AudioClip enemyDiesSfx;
    public AudioClip bossDiesSfx;

    public AudioClip gamePausedSfx;
    public AudioClip gameUnpausedSfx;

    public AudioClip turretShootSfx;

    public AudioClip normalTeleporterSfx;
    public AudioClip bossTeleporterSfx;
    
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

            EnsureDefaultClips();
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.loop = true;
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // force 2D
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D UI SFX
            }

            audioSource.volume = musicVolume;
            sfxSource.volume = sfxVolume;
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
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        UpdateAllSFXVolume();
    }

    public void SetUISfxClips(AudioClip hover, AudioClip click)
    {
        if (hover != null) uiHoverSfx = hover;
        if (click != null) uiClickSfx = click;
    }

    public void SetGameplaySfxClips(AudioClip walking, AudioClip meleeSwing)
    {
        if (walking != null) playerWalkingSfx = walking;
        if (meleeSwing != null) playerMeleeSwingSfx = meleeSwing;
    }

    public void SetPlayerCombatSfxClips(AudioClip bowShoot, AudioClip staffUse, AudioClip playerDamage)
    {
        if (bowShoot != null) playerBowShootSfx = bowShoot;
        if (staffUse != null) playerStaffUseSfx = staffUse;
        if (playerDamage != null) playerTookDamageSfx = playerDamage;
    }

    public void SetEnemyCombatSfxClips(AudioClip enemyBow, AudioClip enemySword)
    {
        if (enemyBow != null) enemyBowShootSfx = enemyBow;
        if (enemySword != null) enemySwordSwingSfx = enemySword;
    }

    public void SetPerkSfxClips(AudioClip perkSelect)
    {
        if (perkSelect != null) perkSelectedSfx = perkSelect;
    }

    public void SetEnemyDamageSfxClips(AudioClip skeletonDamage, AudioClip slimeDamage)
    {
        if (skeletonDamage != null) skeletonTookDamageSfx = skeletonDamage;
        if (slimeDamage != null) slimeEnemyTookDamageSfx = slimeDamage;
    }

    public void SetBossDamageSfxClips(AudioClip slimeBossDamage, AudioClip leechBossDamage, AudioClip golemBossDamage)
    {
        if (slimeBossDamage != null) slimeBossTookDamageSfx = slimeBossDamage;
        if (leechBossDamage != null) leechBossTookDamageSfx = leechBossDamage;
        if (golemBossDamage != null) golemBossTookDamageSfx = golemBossDamage;
    }

    public void SetTeleporterSfxClips(AudioClip normalTeleport, AudioClip bossTeleport)
    {
        if (normalTeleport != null) normalTeleporterSfx = normalTeleport;
        if (bossTeleport != null) bossTeleporterSfx = bossTeleport;
    }

    public void SetAdditionalSfxClips(
        AudioClip dash,
        AudioClip enemyDies,
        AudioClip bossDies,
        AudioClip playerDies,
        AudioClip pause,
        AudioClip unpause,
        AudioClip xpPickup,
        AudioClip weaponPickup,
        AudioClip medkitHeal,
        AudioClip turretShoot)
    {
        if (dash != null) playerDashSfx = dash;
        if (enemyDies != null) enemyDiesSfx = enemyDies;
        if (bossDies != null) bossDiesSfx = bossDies;
        if (playerDies != null) playerDiesGameOverSfx = playerDies;
        if (pause != null) gamePausedSfx = pause;
        if (unpause != null) gameUnpausedSfx = unpause;
        if (xpPickup != null) xpPickupSfx = xpPickup;
        if (weaponPickup != null) weaponPickupSfx = weaponPickup;
        if (medkitHeal != null) playerMedkitHealingSfx = medkitHeal;
        if (turretShoot != null) turretShootSfx = turretShoot;
    }

    public void PlayUIHoverSfx()
    {
        if (uiHoverSfx == null) return;
        if (sfxSource == null) return;
        sfxSource.PlayOneShot(uiHoverSfx, sfxVolume);
    }

    public void PlayUIClickSfx()
    {
        if (uiClickSfx == null) return;
        if (sfxSource == null) return;
        sfxSource.PlayOneShot(uiClickSfx, sfxVolume);
    }

    public void PlaySfx(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null) return;
        if (sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * Mathf.Clamp01(volumeMultiplier));
    }

    public void PlaySpatialSfx(AudioClip clip, Vector3 position, float volumeMultiplier = 1f, float minDistance = 2f, float maxDistance = 25f)
    {
        if (clip == null) return;

        // One-shot spatial audio: create a temp AudioSource at the position.
        GameObject temp = new GameObject("TempSpatialSfx_" + clip.name);
        temp.transform.position = position;

        AudioSource src = temp.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.dopplerLevel = 0f;

        src.PlayOneShot(clip, sfxVolume * Mathf.Clamp01(volumeMultiplier));
        Destroy(temp, clip.length + 0.1f);
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

    private void EnsureDefaultUISfx()
    {
        // Fallback for cases where scenes don't assign these via inspector.
        // Works reliably in Editor; in builds it depends on whether clips are included in the player.
        if (uiHoverSfx == null) uiHoverSfx = FindClipByName("sfx_hover_ui_button");
        if (uiClickSfx == null) uiClickSfx = FindClipByName("sfx_click_ui_button");
    }

    private void EnsureDefaultBgm()
    {
        // Same idea as UI SFX: use clip names that match the filenames (without .mp3)
        if (mainMenuMusic == null) mainMenuMusic = FindClipByName("bgm_main_menu");
        if (gameplayMusic == null) gameplayMusic = FindClipByName("bgm_default_playing_game");
        if (bossMusic == null) bossMusic = FindClipByName("bgm_boss_fight");
    }

    private void EnsureDefaultClips()
    {
        EnsureDefaultBgm();
        EnsureDefaultUISfx();
        EnsureDefaultGameplaySfx();
    }

    private void EnsureDefaultGameplaySfx()
    {
        if (playerWalkingSfx == null) playerWalkingSfx = FindClipByName("sfx_player_is_walking");
        if (playerMeleeSwingSfx == null) playerMeleeSwingSfx = FindClipByName("sfx_sword_swing");
        if (playerBowShootSfx == null) playerBowShootSfx = FindClipByName("sfx_bow_release");
        if (playerStaffUseSfx == null) playerStaffUseSfx = FindClipByName("sfx_staff_was_used_and_explodes");
        if (playerTookDamageSfx == null) playerTookDamageSfx = FindClipByName("sfx_player_took_damage");

        if (playerDashSfx == null) playerDashSfx = FindClipByName("sfx_player_is_dashing");
        if (playerDiesGameOverSfx == null) playerDiesGameOverSfx = FindClipByName("sfx_player_dies_game_over");

        if (enemyBowShootSfx == null) enemyBowShootSfx = FindClipByName("sfx_bow_release_enemy");
        if (enemySwordSwingSfx == null) enemySwordSwingSfx = FindClipByName("sfx_sword_swing_enemy");

        if (perkSelectedSfx == null) perkSelectedSfx = FindClipByName("sfx_perk_was_selected");

        if (xpPickupSfx == null) xpPickupSfx = FindClipByName("sfx_player_picks_up_xp");
        if (weaponPickupSfx == null) weaponPickupSfx = FindClipByName("sfx_player_picked_up_weapon");
        if (playerMedkitHealingSfx == null) playerMedkitHealingSfx = FindClipByName("sfx_player_medkit_healing");

        if (skeletonTookDamageSfx == null) skeletonTookDamageSfx = FindClipByName("sfx_skeleton_took_damage");
        if (slimeEnemyTookDamageSfx == null) slimeEnemyTookDamageSfx = FindClipByName("sfx_slime_enemy_took_damage");
        if (slimeBossTookDamageSfx == null) slimeBossTookDamageSfx = FindClipByName("sfx_slime_boss_took_damage");
        if (leechBossTookDamageSfx == null) leechBossTookDamageSfx = FindClipByName("sfx_leech_boss_took_damage");
        if (golemBossTookDamageSfx == null) golemBossTookDamageSfx = FindClipByName("sfx_golem_boss_took_damage");

        if (enemyDiesSfx == null) enemyDiesSfx = FindClipByName("sfx_enemy_dies");
        if (bossDiesSfx == null) bossDiesSfx = FindClipByName("sfx_boss_dies");

        if (gamePausedSfx == null) gamePausedSfx = FindClipByName("sfx_game_was_paused");
        if (gameUnpausedSfx == null) gameUnpausedSfx = FindClipByName("sfx_game_was_unpaused");

        if (turretShootSfx == null) turretShootSfx = FindClipByName("sfx_turret_is_shooting_arrow");

        if (normalTeleporterSfx == null) normalTeleporterSfx = FindClipByName("sfx_normal_teleporter");
        if (bossTeleporterSfx == null) bossTeleporterSfx = FindClipByName("sfx_boss_teleporter");
    }

    public static AudioClip FindClipByName(string clipName)
    {
        AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
        for (int i = 0; i < clips.Length; i++)
        {
            AudioClip clip = clips[i];
            if (clip != null && clip.name == clipName) return clip;
        }
        return null;
    }
}
