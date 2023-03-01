using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

public class GameConfigManager : MonoBehaviour
{
    public static string GameConfigPath;

    public AudioMixer m_AudioMixer;

    private GameConfig m_newConfig;


    public GameConfig NewConfig { get => m_newConfig; }


    private void Awake()
    {
        m_newConfig = null;

        InitGameConfig();
        CheckDisplayResolution();
    }

    public void SetPlayerName(string newPlayerName)
    {
        if (string.IsNullOrEmpty(newPlayerName))
            newPlayerName = "Player";

        CreateNewConfigIfRequired();

        GameCtrl.Instance.m_PlayerName = newPlayerName;
        m_newConfig.m_PlayerName = newPlayerName;
    }

    public void SetResolution(int selection)
    {
        var selected = Screen.resolutions[selection];

        CreateNewConfigIfRequired();

        m_newConfig.m_ResWidth = selected.width;
        m_newConfig.m_ResHeight = selected.height;
        m_newConfig.m_RefreshRate = selected.refreshRate;
    }

    public void SetFullScreen(bool value)
    {
        CreateNewConfigIfRequired();

        m_newConfig.m_FullScreen = value;
    }

    public void SetTextPrintSpeed(float spd)
    {
        CreateNewConfigIfRequired();

        m_newConfig.m_PrintSpd = (int)spd;
    }

    public void SetBgmVolume(int level)
    {
        float log = GetLogarithmicVolumeLvl(level);
        m_AudioMixer.SetFloat("BgmVolume", log);
    }

    public void SetSfxVolume(int level)
    {
        float log = GetLogarithmicVolumeLvl(level);
        m_AudioMixer.SetFloat("SfxVolume", log);
    }

    public void SetMasterVolumeLevel(float level)
    {
        AudioListener.volume = level / 100.0f;

        CreateNewConfigIfRequired();

        m_newConfig.m_MasterVolume = (int)level;
    }

    public void SetBgmVolumeLevel(float level)
    {
        SetBgmVolume((int)level);
        CreateNewConfigIfRequired();

        m_newConfig.m_BgmVolume = (int)level;
    }

    public void SetSfxVolumeLevel(float level)
    {
        SetSfxVolume((int)level);
        CreateNewConfigIfRequired();

        m_newConfig.m_SfxVolume = (int)level;
    }

    public void CancelChanges()
    {
        AudioListener.volume = Globals.m_GameConfig.m_MasterVolume / 100.0f;
        SetBgmVolume(Globals.m_GameConfig.m_BgmVolume);
        SetSfxVolume(Globals.m_GameConfig.m_SfxVolume);

        m_newConfig = null;
    }

    public void Apply()
    {
        if (m_newConfig != null)
        {
            Globals.m_GameConfig.OverwriteFrom(m_newConfig);
            Apply(m_newConfig);
            m_newConfig = null;
        }
    }

    public void Apply(GameConfig gameConfig)
    {
        Screen.SetResolution(gameConfig.m_ResWidth, gameConfig.m_ResHeight, gameConfig.m_FullScreen);
        GameConfig.WriteToFile(gameConfig, Globals.GetConfigFilePath());
    }

    private void InitGameConfig()
    {
        // create the game save storage path
        if (!Directory.Exists(Consts.SaveGameStorePath))
            Directory.CreateDirectory(Consts.SaveGameStorePath);

        GameConfigPath = Consts.SaveGameStorePath + "usercfg.xml";

        if (!File.Exists(GameConfigPath))
        {
            // if no game config file exists, create one now!!
            Globals.m_GameConfig.m_ResWidth = Screen.currentResolution.width;
            Globals.m_GameConfig.m_ResHeight = Screen.currentResolution.height;
            Globals.m_GameConfig.m_RefreshRate = Screen.currentResolution.refreshRate;
            Globals.m_GameConfig.m_FullScreen = true;

            // serialize the default game config file as the new usercfg file
            GameConfig.WriteToFile(Globals.m_GameConfig, GameConfigPath);
        }
        else
        {
            // otherwise just load it from file and apply the game config
            if (!GameConfig.LoadFromFile(ref Globals.m_GameConfig, GameConfigPath))
                throw new ApplicationException("Failed to load game config file.");
        }
    }

    private void CheckDisplayResolution()
    {
        // check if the resolution attached to the current game config is valid
        // apply supported resolution if unsupported resolution is found in game config
        bool isValidResolution = false;
        for (int i = 0; i < Globals.m_SupportedResolutions.Count; i++)
        {
            var res = Globals.m_SupportedResolutions[i];

            if (res.width == Globals.m_GameConfig.m_ResWidth &&
                res.height == Globals.m_GameConfig.m_ResHeight)
            {
                isValidResolution = true;
            }
        }

        if (!isValidResolution)
        {
            Debug.Log("invalid resolution, setting highest supported resolution...");

            // find the highest supported resolution and set that
            var bestSupportedRes = Globals.m_SupportedResolutions[Screen.resolutions.Length - 1];

            Globals.m_GameConfig.m_ResWidth = bestSupportedRes.width;
            Globals.m_GameConfig.m_ResHeight = bestSupportedRes.height;
        }

        // apply game config to system
        Apply(Globals.m_GameConfig);
    }

    private void CreateNewConfigIfRequired()
    {
        if (m_newConfig == null)
        {
            m_newConfig = new GameConfig(Globals.m_GameConfig);
        }
    }

    private float GetLogarithmicVolumeLvl(float level)
    {
        if (level <= Mathf.Epsilon)
            level = Mathf.Epsilon;

        float log10 = Mathf.Log10(level);
        float result = log10 * 20.0f;

        return result;
    }

    private float GetLogarithmicVolumeLvl(int level)
    {
        float normalizedLevel = level / 100.0f;

        if (level == 0)
            normalizedLevel = Mathf.Epsilon;

        float log10 = Mathf.Log10(normalizedLevel);
        float result = log10 * 20.0f;

        return result;
    }
}
