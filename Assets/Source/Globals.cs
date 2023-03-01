using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Globals
{
    public static bool m_DevMode = true;          // make sure this is false when set for release

    // global system settings
    public static GameConfig m_GameConfig = new GameConfig();
    public static List<Resolution> m_SupportedResolutions = new List<Resolution>();
    public static int m_PrintSpd = 60;

    // game load parameters
    public static EGameType m_SelectedGameType = EGameType.Singleplay;

    public static uint m_ConnectGameType;    // 0 - server only, 1 - host, 2 - client 
    public static MapListEntry m_CurrentMap = null;
    public static E3DServerResponse m_SelectedTargetServer = null;
    //public static int m_NumVictims = Consts.VICTIM_COUNT_DEFAULT;
    //public static int m_Probability_P3 = Consts.VICTIM_PROB_P3_DEFAULT;
    //public static int m_Probability_P2 = Consts.VICTIM_PROB_P2_DEFAULT;
    //public static int m_Probability_P1 = Consts.VICTIM_PROB_P1_DEFAULT;
    //public static int m_Probability_P0 = Consts.VICTIM_PROB_P0_DEFAULT;

    public static readonly Dictionary<EEmtRole, string> m_EmtRoleStrings = new Dictionary<EEmtRole, string>
    {
        { EEmtRole.Spectator, "Spectator" },
        { EEmtRole.IncidentCmdr, "Incident Commander" },
        { EEmtRole.TriageOffr, "Triage Officer" },
        { EEmtRole.FirstAidPointDoc, "First Aid Point Doctor" },
        { EEmtRole.EvacOffr, "Evacuation Officer" }
    };


    public static string Hostname
    {
        get
        {
            if (GameCtrl.Instance == null)
                return "New Multiplayer Game";

            return GameCtrl.Instance.m_Hostname;
        }
    }

    public static string CommanderName
    {
        get { return GameCtrl.Instance.m_CmdrName; }
    }

    /// <summary>
    /// Quits the game. If the game is in Unity Editor mode, the play mode will just stop.
    /// </summary>
    public static void QuitGame()
    {
#if UNITY_EDITOR
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }
#else
        Application.Quit();
#endif
    }

    public static string GetConfigFilePath()
    {
        //if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
        //{
        //    gameDataPath += "..\\..\\EmuUserData\\";
        //    if (!Directory.Exists(gameDataPath))
        //        Directory.CreateDirectory(gameDataPath);
        //}

        return Consts.SaveGameStorePath + "usercfg.xml";
    }
}
