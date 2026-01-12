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
        // check if manager exists so we dont crash
        if (MusicManager.Instance != null)
        {
            // set slider values to what we have saved
            musicSlider.value = MusicManager.Instance.musicVolume;
            sfxSlider.value = MusicManager.Instance.sfxVolume;


        }
        // add listeners so when we drag slider it changes volume real time
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        // verify manager again
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetMusicVolume(value);
        }
    }

    public void SetSFXVolume(float value)
    {
        // verify manager for sfx
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetSFXVolume(value);
        }
    }
}