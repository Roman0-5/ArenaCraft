using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Linq;

namespace ArenaCraft
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("Audio")]
        public AudioMixer mainMixer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LoadSettings()
        {
            // Audio
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
            float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
            SetVolume("MasterVolume", masterVol);
            SetVolume("MusicVolume", musicVol);
            SetVolume("SFXVolume", sfxVol);

            // Graphics
            int quality = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            QualitySettings.SetQualityLevel(quality, true);

            int fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
            Screen.fullScreen = fullscreen == 1;

            // Resolution is tricky to set here as it depends on available resolutions
        }

        public void SetVolume(string parameter, float value)
        {
            PlayerPrefs.SetFloat(parameter, value);
            if (mainMixer != null)
            {
                // Convert 0-1 to dB (-80 to 20)
                float dB = value <= 0 ? -80f : Mathf.Log10(value) * 20f;
                mainMixer.SetFloat(parameter, dB);
            }
            else
            {
                // Fallback: If no mixer, at least Master can affect AudioListener
                if (parameter == "MasterVolume") AudioListener.volume = value;
            }
        }

        public void SetQuality(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
            PlayerPrefs.SetInt("QualityLevel", level);
        }

        public void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        }

        public void SetResolution(int width, int height, bool isFullscreen)
        {
            Screen.SetResolution(width, height, isFullscreen);
            PlayerPrefs.SetInt("ResWidth", width);
            PlayerPrefs.SetInt("ResHeight", height);
        }

        public void SetSplitScreen(bool enabled)
        {
            PlayerPrefs.SetInt(SplitScreenManager.PreferenceKey, enabled ? 1 : 0);
            SplitScreenManager splitScreen = Object.FindAnyObjectByType<SplitScreenManager>();
            if (splitScreen != null) splitScreen.SetSplitScreen(enabled);
        }
    }
}
