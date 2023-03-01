using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Mirror;
using E3D;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public enum EGameType { Singleplay = 0, Multiplay = 1 }

public enum EDifficulty { Easy = 0, Medium = 1, Hard = 2 }

public enum EEmtSelection { Spectator = 0, TriageOffr = 1, FirstAidPointDoc = 2, EvacOffr = 3 }

public enum EEmtRole { Spectator = 0, IncidentCmdr = 1, TriageOffr = 2, FirstAidPointDoc = 3, EvacOffr = 4 }


// Victim Settings
// Settings for how victims will be generated in the game
[Serializable]
public struct VictimSettings_s
{
    // number of victims that will be created for this map
    [Range(1, 100)]
    public int m_NumVictims;
    [Range(0, 100)]
    public int m_Probability_P3;
    [Range(0, 100)]
    public int m_Probability_P2;
    [Range(0, 100)]
    public int m_Probability_P1;
    [Range(0, 100)]
    public int m_Probability_P0;
    [Range(1, 10)]
    public int m_AgeGap;


    public VictimSettings_s(int victimCount, int prob_p0, int prob_p1, int prob_p2, int prob_p3, int ageGap)
    {
        m_NumVictims = victimCount < 1 ? 1 : victimCount;
        m_Probability_P0 = prob_p0;
        m_Probability_P1 = prob_p1;
        m_Probability_P2 = prob_p2;
        m_Probability_P3 = prob_p3;
        m_AgeGap = ageGap <= 0 ? 5 : ageGap;
    }
}


[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkAuthenticator), typeof(E3DNetDiscovery))]
public class GameCtrl : NetworkRoomManager
{
    [HideInInspector]
    public NetworkAuthenticator m_NetAuth;
    [HideInInspector]
    public E3DNetDiscovery m_NetDiscovery;
    [HideInInspector]
    public GameConfigManager m_GameConfigManager;

    public readonly EGameType m_GameType = EGameType.Multiplay;

    [Header("Game Settings")]
    public string m_PlayerName = "unnamed";
    public EDifficulty m_Difficulty = EDifficulty.Easy;
    public VictimSettings_s m_VictimSettings = new VictimSettings_s {
        m_NumVictims = Consts.VICTIM_COUNT_DEFAULT,
        m_Probability_P0 = Consts.VICTIM_PROB_P0_DEFAULT,
        m_Probability_P1 = Consts.VICTIM_PROB_P1_DEFAULT,
        m_Probability_P2 = Consts.VICTIM_PROB_P2_DEFAULT,
        m_Probability_P3 = Consts.VICTIM_PROB_P3_DEFAULT,
        m_AgeGap = Consts.VICTIM_AGE_GAP_DEFAULT
    };
    public EEmtSelection m_SelectRoleOverride = EEmtSelection.Spectator;
    [ReadOnlyVar]
    public EEmtRole m_SelectedRole = EEmtRole.TriageOffr;
    public bool m_EnableBots = false;

    [Header("Server Settings (Host)")]
    public string m_Hostname = "New Multiplayer Game";
    public string m_CmdrName = "unnamed";

    [Header("Client Settings (Client)")]
    public string m_ConnIpAddr = "localhost";

    [Header("Debug")]
    public bool m_DbgDisableGameStartCheck = false;
    public bool m_DbgTriageAllVictims = false;
    public bool m_DbgTreatAllVictims = false;

    private static MapListEntry[] m_MapList;

    private GameMode m_curGameMode = null;

    public event UnityAction onLobbyPlayerEnter;
    public event UnityAction onLobbyPlayerExit;
    public event UnityAction onServerDisconnect;
    public event UnityAction onClientDisconnect;
    public event UnityAction<Exception> onServerError;
    public event UnityAction<Exception> onClientError;


    public static GameCtrl Instance { get => (GameCtrl)singleton; }

    public static MapListEntry[] MapList { get => m_MapList; }

    public GameMode CurrentGameMode { get => m_curGameMode; }


    /// <summary>
    /// Load up the game system regardless of which scene is loaded
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Sys_BeforeSceneLoad()
    {
        // if the game system has not been initialised, then we must load the "sys_main" scene file
        if (!Instance)
        {
            Scene rootScene = SceneManager.GetSceneByName("sys_main");
            if (rootScene.buildIndex == -1)
                SceneManager.LoadScene("sys_main", LoadSceneMode.Additive);

            // build resolution list
            // reference: https://answers.unity.com/questions/1463609/screenresolutions-returning-duplicates.html
            if (Globals.m_SupportedResolutions.Count == 0)
            {
                HashSet<Resolution> resFiltered = new HashSet<Resolution>();
                foreach (var r in Screen.resolutions)
                {
                    Resolution newRes = new Resolution();
                    newRes.width = r.width;
                    newRes.height = r.height;
                    newRes.refreshRate = 0;

                    resFiltered.Add(newRes);
                }

                //var resFiltered = Screen.resolutions
                //    .GroupBy(r => new { r.width, r.height })
                //    .Select(grp => new Resolution { width = grp.Key.width, height = grp.Key.height, refreshRate = 0 });
                var resolutions = resFiltered.ToArray();

                for (int i = 0; i < resolutions.Length; i++)
                {
                    Globals.m_SupportedResolutions.Add(resolutions[i]);
                }
            }

            return;
        }
    }

    public override void Awake()
    {
        if (Instance != null && Instance == this)
            return;

        base.Awake();

        DOTween.Init();

        m_NetDiscovery = GetComponent<E3DNetDiscovery>();
        m_GameConfigManager = GetComponent<GameConfigManager>();
    }

    private void Reset()
    {
        m_Difficulty = EDifficulty.Easy;

        maxConnections = 4;
        m_VictimSettings.m_NumVictims = Consts.VICTIM_COUNT_DEFAULT;
        m_VictimSettings.m_Probability_P0 = Consts.VICTIM_PROB_P0_DEFAULT;
        m_VictimSettings.m_Probability_P1 = Consts.VICTIM_PROB_P1_DEFAULT;
        m_VictimSettings.m_Probability_P2 = Consts.VICTIM_PROB_P2_DEFAULT;
        m_VictimSettings.m_Probability_P3 = Consts.VICTIM_PROB_P3_DEFAULT;
        m_VictimSettings.m_AgeGap = Consts.VICTIM_AGE_GAP_DEFAULT;
    }

    public override void Start()
    {
        // populate map list
        if (m_MapList == null)
            m_MapList = Resources.LoadAll<MapListEntry>("MapList");

        // set the player name from the usercfg thing
        m_PlayerName = Globals.m_GameConfig.m_PlayerName;
    }

    public void CreateLobby()
    {
        StartHost();
        m_NetDiscovery.AdvertiseServer();
    }

    public void JoinLobby(Uri uri)
    {
        StartClient(uri);
    }

    public void StartGame()
    {
        bool canStartGame = CheckStartGameCondition();
        if (!canStartGame)
        {
            // cannot start game, inform all conencted clients that we can't join the game
            // until one of each role is available in the game
            for (int i = 0; i < roomSlots.Count; i++)
            {
                var lobbyPlayer = (E3DLobbyPlayer)roomSlots[i];
                lobbyPlayer.CMD_InformGameCannotStart(
                    "The game cannot start. There must be at least one Incident Commander, " +
                    "one Triage Officer, one First Aid Point Doctor and one Evacuation Officer.");
            }

            return;
        }

        ServerChangeScene(GameplayScene);
    }

    public bool CheckStartGameCondition()
    {
        if (m_DbgDisableGameStartCheck)
            return true;

        bool hasIncidentCmdr = false;
        bool hasTriageOffr = false;
        bool hasFirstAidDoc = false;
        bool hasEvacOffr = false;

        // make sure to inform all other connected clients as well
        for (int i = 0; i < roomSlots.Count; i++)
        {
            var lobbyPlayer = (E3DLobbyPlayer)roomSlots[i];
            if (lobbyPlayer.m_SelectedRole == EEmtRole.IncidentCmdr)
                hasIncidentCmdr = true;
            else if (lobbyPlayer.m_SelectedRole == EEmtRole.TriageOffr)
                hasTriageOffr = true;
            else if (lobbyPlayer.m_SelectedRole == EEmtRole.FirstAidPointDoc)
                hasFirstAidDoc = true;
            else if (lobbyPlayer.m_SelectedRole == EEmtRole.EvacOffr)
                hasEvacOffr = true;
        }

        return hasIncidentCmdr && hasTriageOffr && hasFirstAidDoc && hasEvacOffr;
    }

    public void Disconnect()
    {
        if (mode == NetworkManagerMode.Host)
            StopHost();
        else if (mode == NetworkManagerMode.ClientOnly)
            StopClient();
    }

    /// <summary>
    /// Spawns a gameobject from the prefab list by prefab name.
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

        GameObject foundPrefab = FindPrefab(prefabName);
        if (foundPrefab == null)
        {
            Debug.Log("no prefab with name " + prefabName + " can be found");
            return null;
        }

        GameObject newNetObject = Instantiate(foundPrefab);
        NetworkServer.Spawn(newNetObject, owner);

        return newNetObject;
    }

    public GameObject FindPrefab(string prefabName)
    {
        for (int i = 0; i < spawnPrefabs.Count; i++)
        {
            if (spawnPrefabs[i].name == prefabName)
            {
                return spawnPrefabs[i];
            }
        }

        return null;
    }

    public GameMode GetGameMode() { return m_curGameMode; }

    public T GetGameMode<T>() where T : GameMode
    {
        if (!(m_curGameMode is T))
            return null;

        return (T)m_curGameMode;
    }


    #region Server Callbacks

    /// <summary>
    /// This is called on the host when a host is started.
    /// </summary>
    public override void OnRoomStartHost()
    {
        // if you are host, set your player name as commander name to globals (which is to be 
        // propagated to all connected clients)
        m_CmdrName = m_PlayerName;
    }

    /// <summary>
    /// This is called on the host when the host is stopped.
    /// </summary>
    public override void OnRoomStopHost() { }

    /// <summary>
    /// This is called on the server when a new client connects to the server.
    /// </summary>
    /// <param name="conn">The new connection.</param>
    public override void OnRoomServerConnect(NetworkConnection conn) { }

    /// <summary>
    /// This is called on the server when a client disconnects.
    /// </summary>
    /// <param name="conn">The connection that disconnected.</param>
    public override void OnRoomServerDisconnect(NetworkConnection conn)
    {
        // if game is in progress, then we can replace them with bots
        if (m_curGameMode != null && m_curGameMode.m_State.m_CurrentMatchState != EMatchState.GameOver)
        {
            // replace player with new one
            GameObject oldPlayerObject = conn.identity.gameObject;
            E3DPlayer oldPlayer = oldPlayerObject.GetComponent<E3DPlayer>();

            GameObject newPlayerObject = null;
            E3DPlayer newPlayer = null;
            if (oldPlayer is E3DTriageOffrPlayer)
            {
                newPlayerObject = Instantiate(FindPrefab("AE3DCpu_TriageOffr"));
            }
            else if (oldPlayer is E3DFirstAidDocPlayer)
            {
                newPlayerObject = Instantiate(FindPrefab("AE3DCpu_FirstAidDoc"));
            }
            else if (oldPlayer is E3DEvacOffrPlayer)
            {
                newPlayerObject = Instantiate(FindPrefab("AE3DCpu_EvacOffr"));
            }

            if (newPlayerObject != null)
            {
                NetworkServer.Spawn(newPlayerObject);

                newPlayer = newPlayerObject.GetComponent<E3DPlayer>();
                newPlayer.SV_AssumeControl(oldPlayerObject);

                //NetworkServer.ReplacePlayerForConnection(conn, newPlayerObject);
                //NetworkServer.Destroy(oldPlayerObject);
            }
        }
    }

    /// <summary>
    /// This is called on the server when a networked scene finishes loading.
    /// </summary>
    /// <param name="sceneName">Name of the new scene.</param>
    public override void OnRoomServerSceneChanged(string sceneName)
    {
        // spawn the game mode on scene change
        if (sceneName == GameplayScene)
        {
            // spawn the game rules
            GameObject gameModeObject = SV_Spawn("GameMode");
            if (gameModeObject == null)
            {
                // TODO: disconnect and get da fuck outta here
                return;
            }

            m_curGameMode = gameModeObject.GetComponent<GameMode>();
            m_curGameMode.m_DbgTreatAllVictims = m_DbgTreatAllVictims;
            m_curGameMode.m_DbgTriageAllVictims = m_DbgTriageAllVictims;
        }
        else
        {
            if (m_curGameMode != null)
            {
                NetworkServer.Destroy(m_curGameMode.gameObject);
                Destroy(m_curGameMode.gameObject);
            }    
        }
    }

    /// <summary>
    /// This allows customization of the creation of the room-player object on the server.
    /// <para>By default the roomPlayerPrefab is used to create the room-player, but this function allows that behaviour to be customized.</para>
    /// </summary>
    /// <param name="conn">The connection the player object is for.</param>
    /// <returns>The new room-player object.</returns>
    public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnection conn)
    {
        return base.OnRoomServerCreateRoomPlayer(conn);
    }

    /// <summary>
    /// This allows customization of the creation of the GamePlayer object on the server.
    /// <para>By default the gamePlayerPrefab is used to create the game-player, but this function allows that behaviour to be customized. The object returned from the function will be used to replace the room-player on the connection.</para>
    /// </summary>
    /// <param name="conn">The connection the player object is for.</param>
    /// <param name="roomPlayer">The room player object for this connection.</param>
    /// <returns>A new GamePlayer object.</returns>
    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnection conn, GameObject roomPlayer)
    {
        E3DLobbyPlayer lobbyPlayer = roomPlayer.GetComponent<E3DLobbyPlayer>();

        GameObject gamePlayer;
        switch (lobbyPlayer.m_SelectedRole)
        {
            case EEmtRole.TriageOffr:
                gamePlayer = SV_Spawn("AE3DPlayer_TriageOffr");
                break;

            case EEmtRole.FirstAidPointDoc:
                gamePlayer = SV_Spawn("AE3DPlayer_FirstAidDoc");
                break;

            case EEmtRole.EvacOffr:
                gamePlayer = SV_Spawn("AE3DPlayer_EvacOffr");
                break;

            case EEmtRole.IncidentCmdr:
                gamePlayer = SV_Spawn("AE3DPlayer_IncidentCmdr");
                break;

            default:
                gamePlayer = null;
                break;
        }

        return gamePlayer;
    }

    /// <summary>
    /// This allows customization of the creation of the GamePlayer object on the server.
    /// <para>This is only called for subsequent GamePlay scenes after the first one.</para>
    /// <para>See OnRoomServerCreateGamePlayer to customize the player object for the initial GamePlay scene.</para>
    /// </summary>
    /// <param name="conn">The connection the player object is for.</param>
    public override void OnRoomServerAddPlayer(NetworkConnection conn)
    {
        base.OnRoomServerAddPlayer(conn);
    }

    /// <summary>
    /// This is called on the server when it is told that a client has finished switching from the room scene to a game player scene.
    /// <para>When switching from the room, the room-player is replaced with a game-player object. This callback function gives an opportunity to apply state from the room-player to the game-player object.</para>
    /// </summary>
    /// <param name="conn">The connection of the player</param>
    /// <param name="roomPlayer">The room player object.</param>
    /// <param name="gamePlayer">The game player object.</param>
    /// <returns>False to not allow this player to replace the room player.</returns>
    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnection conn, GameObject roomPlayer, GameObject gamePlayer)
    {
        return base.OnRoomServerSceneLoadedForPlayer(conn, roomPlayer, gamePlayer);
    }

    /// <summary>
    /// This is called on the server when all the players in the room are ready.
    /// <para>The default implementation of this function uses ServerChangeScene() to switch to the game player scene. By implementing this callback you can customize what happens when all the players in the room are ready, such as adding a countdown or a confirmation for a group leader.</para>
    /// </summary>
    public override void OnRoomServerPlayersReady() { }

    /// <summary>
    /// This is called on the server when CheckReadyToBegin finds that players are not ready
    /// <para>May be called multiple times while not ready players are joining</para>
    /// </summary>
    public override void OnRoomServerPlayersNotReady() { }

    #endregion


    #region Client Callbacks

    /// <summary>
    /// This is a hook to allow custom behaviour when the game client enters the room.
    /// </summary>
    public override void OnRoomClientEnter()
    {
        if (onLobbyPlayerEnter != null)
            onLobbyPlayerEnter();
    }

    /// <summary>
    /// This is a hook to allow custom behaviour when the game client exits the room.
    /// </summary>
    public override void OnRoomClientExit()
    {
        if (onLobbyPlayerExit != null)
            onLobbyPlayerExit();
    }

    /// <summary>
    /// This is called on the client when it connects to server.
    /// </summary>
    /// <param name="conn">The connection that connected.</param>
    public override void OnRoomClientConnect(NetworkConnection conn) { }

    /// <summary>
    /// This is called on the client when disconnected from a server.
    /// </summary>
    /// <param name="conn">The connection that disconnected.</param>
    public override void OnRoomClientDisconnect(NetworkConnection conn)
    {
        
    }

    /// <summary>
    /// This is called on the client when the client is finished loading a new networked scene.
    /// </summary>
    /// <param name="conn">The connection that finished loading a new networked scene.</param>
    public override void OnRoomClientSceneChanged(NetworkConnection conn) { }

    /// <summary>
    /// Called on the client when adding a player to the room fails.
    /// <para>This could be because the room is full, or the connection is not allowed to have more players.</para>
    /// </summary>
    public override void OnRoomClientAddPlayerFailed() { }

    #endregion


    #region Disconnection and Errors

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);

        if (onServerDisconnect != null)
            onServerDisconnect();
    }

    public override void OnServerError(NetworkConnection conn, Exception exception)
    {
        if (onServerError != null)
            onServerError(exception);
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);

        if (onClientDisconnect != null)
            onClientDisconnect();
    }

    public override void OnClientError(Exception exception)
    {
        if (onClientError != null)
            onClientError(exception);
    }

    #endregion


    public override void OnValidate()
    {
        base.OnValidate();

        m_NetDiscovery = GetComponent<E3DNetDiscovery>();
        m_GameConfigManager = GetComponent<GameConfigManager>();

        if (m_VictimSettings.m_AgeGap < 1)
            m_VictimSettings.m_AgeGap = 1;
        else if (m_VictimSettings.m_AgeGap > 50)
            m_VictimSettings.m_AgeGap = 50;

        m_SelectedRole = (EEmtRole)((int)m_SelectRoleOverride + 1);
    }
}
