using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuVolumeSliders : MonoBehaviour
{
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        if (MusicManager.Instance != null)
        {
            musicSlider.value = MusicManager.Instance.musicVolume;
            sfxSlider.value = MusicManager.Instance.sfxVolume;


        }
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetMusicVolume(value);
        }
    }

    public void SetSFXVolume(float value)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetSFXVolume(value);
        }
    }
}
