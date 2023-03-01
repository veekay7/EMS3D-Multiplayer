using DG.Tweening;
using E3D;
using Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Game System
[DisallowMultipleComponent]
public class GameSystem : NetworkManager
{
    [HideInInspector]
    public NetworkAuthenticator m_NetAuth;
    [HideInInspector]
    public E3DNetDiscovery m_NetDiscovery;
    [HideInInspector]
    public Transport m_NetTransport;
    [HideInInspector]
    public GameConfigManager m_GameConfigManager;

    public AudioMixer m_AudioMixer;
    public VictimSettings_s m_VictimSettings;
    public List<GameObject> m_LocalSpawnPrefabs = new List<GameObject>();

    private static MapListEntry[] m_MapList;
    private GameMode m_curGameMode;

    public UnityAction<NetworkConnection> onServerAddPlayer;
    public UnityAction<NetworkConnection> onClientDisconnect;


    public static GameSystem Instance { get => (GameSystem)singleton; }

    public static MapListEntry[] MapList { get => m_MapList; }


    public override void Awake()
    {
        if (Instance != null && Instance == this)
            return;

        base.Awake();

        m_NetDiscovery = GetComponent<E3DNetDiscovery>();
        m_NetTransport = GetComponent<Transport>();
        m_GameConfigManager = GetComponent<GameConfigManager>();

        m_VictimSettings = new VictimSettings_s(Consts.VICTIM_COUNT_DEFAULT,
            Consts.VICTIM_PROB_P0_DEFAULT,
            Consts.VICTIM_PROB_P1_DEFAULT,
            Consts.VICTIM_PROB_P2_DEFAULT,
            Consts.VICTIM_PROB_P3_DEFAULT,
            Consts.VICTIM_AGE_GAP_DEFAULT);

        m_curGameMode = null;

        // reigister scene manager events
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
    }

    private void Reset()
    {
        m_VictimSettings.m_NumVictims = Consts.VICTIM_COUNT_DEFAULT;
        m_VictimSettings.m_Probability_P0 = Consts.VICTIM_PROB_P0_DEFAULT;
        m_VictimSettings.m_Probability_P1 = Consts.VICTIM_PROB_P1_DEFAULT;
        m_VictimSettings.m_Probability_P2 = Consts.VICTIM_PROB_P2_DEFAULT;
        m_VictimSettings.m_Probability_P3 = Consts.VICTIM_PROB_P3_DEFAULT;
        m_VictimSettings.m_AgeGap = Consts.VICTIM_AGE_GAP_DEFAULT;
    }

    public override void Start()
    {
        DOTween.Init();

        // create the game save storage path
        if (!Directory.Exists(Consts.SaveGameStorePath))
            Directory.CreateDirectory(Consts.SaveGameStorePath);

        string configFilePath = GetConfigFilePath();

        if (!File.Exists(configFilePath))
        {
            // if no game config file exists, create one now!!
            Globals.m_GameConfig.m_ResWidth = Screen.currentResolution.width;
            Globals.m_GameConfig.m_ResHeight = Screen.currentResolution.height;
            Globals.m_GameConfig.m_RefreshRate = Screen.currentResolution.refreshRate;
            Globals.m_GameConfig.m_FullScreen = true;

            // serialize the default game config file as the new usercfg file
            GameConfig.WriteToFile(Globals.m_GameConfig, configFilePath);
        }
        else
        {
            // otherwise just load it from file and apply the game config
            if (!GameConfig.LoadFromFile(ref Globals.m_GameConfig, configFilePath))
                throw new ApplicationException("Failed to load game config file.");
        }

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
            Debug.LogError("invalid resolution, setting highest supported resolution...");

            // find the highest supported resolution and set that
            var bestSupportedRes = Globals.m_SupportedResolutions[Screen.resolutions.Length - 1];

            Globals.m_GameConfig.m_ResWidth = bestSupportedRes.width;
            Globals.m_GameConfig.m_ResHeight = bestSupportedRes.height;
        }

        // apply game config to system
        m_GameConfigManager.Apply(Globals.m_GameConfig);

        // load all map list entry
        if (m_MapList == null)
            m_MapList = Resources.LoadAll<MapListEntry>("MapList");

        base.Start();
    }

    public GameMode GetGameMode()
    {
        return m_curGameMode;
    }

    public T GetGameMode<T>() where T : GameMode
    {
        if (!(m_curGameMode is T))
            return null;

        return (T)m_curGameMode;
    }


    #region Game Connection

    public void HostGame()
    {
        // TODO: find the machine's local ip address and port on the router
        //SetNetAddrAndPort(Consts.LOCALHOST_ADDRESS, Consts.LOCALHOST_PORT);

        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            StartHost();
            m_NetDiscovery.AdvertiseServer();
        }

        // spawn the game rules
        GameObject gameModeObject = SV_Spawn("GameMode");
        if (gameModeObject == null)
        {
            // TODO: disconnect and get da fuck outta here
            return;
        }

        m_curGameMode = gameModeObject.GetComponent<GameMode>();
    }

    public void Connect()
    {
        if (Globals.m_SelectedTargetServer == null)
            return;

        var targetServer = Globals.m_SelectedTargetServer;

        //SetNetAddrAndPort(targetServer.EndPoint.Address.ToString(), (ushort)targetServer.EndPoint.Port)
        StartClient(targetServer.uri);

        // NOTE: the game mode will automatically be created on client upon connection
    }

    public void SetNetAddrAndPort(string addr, ushort port)
    {
        networkAddress = addr;

        if (m_NetTransport is TelepathyTransport)
        {
            ((TelepathyTransport)m_NetTransport).port = port;
        }
        else if (m_NetTransport is kcp2k.KcpTransport)
        {
            ((kcp2k.KcpTransport)m_NetTransport).Port = port;
        }
    }

    public void DisconnectPlayer(E3DPlayer player)
    {
        Globals.m_ConnectGameType = 0;
        Globals.m_CurrentMap = null;

        if (GUIController.Instance != null)
        {
            if (GUIController.Instance.ActiveScreen != null && GUIController.Instance.ActiveScreen.m_Classname == "Pause")
                GUIController.Instance.CloseCurrentScreen();

            if (GUIController.Instance.ActiveScreen != null && GUIController.Instance.ActiveScreen.m_Classname == "Result")
                GUIController.Instance.CloseCurrentScreen();
        }

        if (ScreenWiper.Instance != null)
            ScreenWiper.Instance.SetFilled(true);

        SceneManager.LoadScene("gamemenu");
    }

    #endregion


    #region Game Objects Management

    /// <summary>
    /// Spawns an object on the client.
    /// </summary>
    /// <param name="prefabName"></param>
    /// <returns></returns>
    public GameObject SpawnLocal(string prefabName)
    {
        for (int i = 0; i < m_LocalSpawnPrefabs.Count; i++)
        {
            if (m_LocalSpawnPrefabs[i].name == prefabName)
            {
                GameObject newLocalObject = Instantiate(m_LocalSpawnPrefabs[i]);
                return newLocalObject;
            }
        }

        Debug.Log("no prefab with name " + prefabName + " can be found");

        return null;
    }

    /// <summary>
    /// Spawns an object on the client and gets the attached component component.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public GameObject SpawnLocal<T>(string prefabName, out T component) where T : MonoBehaviour
    {
        for (int i = 0; i < m_LocalSpawnPrefabs.Count; i++)
        {
            if (m_LocalSpawnPrefabs[i].name == prefabName)
            {
                GameObject newLocalObject = Instantiate(m_LocalSpawnPrefabs[i]);
                component = newLocalObject.GetComponent<T>();

                return newLocalObject;
            }
        }

        Debug.Log("no prefab with name " + prefabName + " can be found");

        component = null;

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="prefabName"></param>
    /// <param name="owner"></param>
    /// <returns></returns>
    public GameObject SV_Spawn(string prefabName, NetworkConnection owner = null)
    {
        if (!NetworkServer.active)
        {
            Debug.Log("network server is not active, cannot spawn prefab with name " + prefabName);
            return null;
        }

        for (int i = 0; i < spawnPrefabs.Count; i++)
        {
            if (spawnPrefabs[i].name == prefabName)
            {
                GameObject newNetObject = Instantiate(spawnPrefabs[i]);
                NetworkServer.Spawn(newNetObject, owner);

                return newNetObject;
            }
        }

        Debug.Log("no prefab with name " + prefabName + " can be found");

        return null;
    }

    /// <summary>
    /// Spawns an object on the server, and will be replicated to all connected clients.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="owner"></param>
    /// <returns></returns>
    public GameObject SV_Spawn<T>(string prefabName, out T behaviour, NetworkConnection owner = null) where T : NetworkBehaviour
    {
        if (!NetworkServer.active)
        {
            Debug.Log("network server is not active, cannot spawn prefab with name " + prefabName);

            behaviour = null;

            return null;
        }

        for (int i = 0; i < spawnPrefabs.Count; i++)
        {
            if (spawnPrefabs[i].name == prefabName)
            {
                GameObject newNetObject = Instantiate(spawnPrefabs[i]);
                NetworkServer.Spawn(newNetObject, owner);

                behaviour = newNetObject.GetComponent<T>();

                return newNetObject;
            }
        }

        Debug.Log("no prefab with name " + prefabName + " can be found");

        behaviour = null;

        return null;
    }

    #endregion


    #region Game Config

    /// <summary>
    /// Returns the path of the game configuration file.
    /// </summary>
    /// <returns></returns>
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

    public void ApplyGameConfigToSystem(GameConfig config)
    {
        Screen.SetResolution(config.m_ResWidth, config.m_ResHeight, config.m_FullScreen);
        AudioListener.volume = config.m_MasterVolume / 100.0f;
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

    #endregion


    #region General Callbacks

    public void SceneManager_activeSceneChanged(Scene oldScene, Scene newScene)
    {
        if ((newScene.name.Equals("dev_guiedit") || newScene.name.Equals("dev_prefabedit") ||
            newScene.name.Equals("gamemenu") || newScene.name.Equals("loading") || newScene.name.Equals("sys_main")))
            return;

        if (Globals.m_CurrentMap == null)
            return;

        string cleanMapFileNameString = Path.GetFileNameWithoutExtension(Globals.m_CurrentMap.m_SceneFilename);

        if (newScene.name.Equals(cleanMapFileNameString))
        {
            m_VictimSettings.m_NumVictims = GameCtrl.Instance.m_VictimSettings.m_NumVictims;
            m_VictimSettings.m_Probability_P3 = GameCtrl.Instance.m_VictimSettings.m_Probability_P3;
            m_VictimSettings.m_Probability_P2 = GameCtrl.Instance.m_VictimSettings.m_Probability_P2;
            m_VictimSettings.m_Probability_P1 = GameCtrl.Instance.m_VictimSettings.m_Probability_P1;
            m_VictimSettings.m_Probability_P0 = GameCtrl.Instance.m_VictimSettings.m_Probability_P0;

            ScreenWiper.Instance.SetFilled(false);

            if (Globals.m_ConnectGameType == 1)
                HostGame();
            else if (Globals.m_ConnectGameType == 2)
                Connect();
        }
    }

    #endregion


    #region Server System Callbacks

    /// <summary>
    /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
    /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        // this only works on the server
        base.OnServerAddPlayer(conn);

        if (onServerAddPlayer != null)
            onServerAddPlayer.Invoke(conn);

        Debug.Log("Num Players: " + numPlayers);
    }


    /// <summary>
    /// Called on the server when a client is ready.
    /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);
        Debug.Log("OnServerReady - Connection from Client: " + conn.connectionId);
    }

    #endregion


    #region Client System Callbacks

    /// <summary>
    /// Called on the client when connected to a server.
    /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        Debug.Log("On Client connected - Connection to the Server: " + conn.connectionId);
    }

    /// <summary>
    /// Called on clients when disconnected from a server.
    /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);

        if (onClientDisconnect != null)
            onClientDisconnect.Invoke(conn);
    }

    #endregion


    public override void OnValidate()
    {
        base.OnValidate();

        m_NetTransport = GetComponent<Transport>();
        m_NetDiscovery = GetComponent<E3DNetDiscovery>();
        m_GameConfigManager = GetComponent<GameConfigManager>();

        if (m_VictimSettings.m_AgeGap < 1)
            m_VictimSettings.m_AgeGap = 1;
        else if (m_VictimSettings.m_AgeGap > 50)
            m_VictimSettings.m_AgeGap = 50;
    }
}
