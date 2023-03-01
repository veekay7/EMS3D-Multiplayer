using Mirror;
using Mirror.Discovery;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

public class E3DServerRequest : NetworkMessage
{
}

public class E3DServerResponse : NetworkMessage
{
    public Uri uri;                 // uri is required so clients know how to connect to the server
    public long serverId;
    public string hostname;
    public string password;
    public string cmdrName;
    public int numPlayers;
    public int maxPlayers;
    public int curMapIndex;
    public string mapName;
    public EDifficulty difficulty;
    public bool isPlaying;


    // The server that sent this
    // this is a property so that it is not serialized,  but the
    // client fills this up after we receive it
    public IPEndPoint EndPoint { get; set; }
}


public class E3DNetDiscovery : NetworkDiscoveryBase<E3DServerRequest, E3DServerResponse>
{
    public bool m_ShowGUI;

    [Tooltip("Transport to be advertised during discovery")]
    public Transport transport;

    private Vector2 m_scrollViewPos;
    private readonly Dictionary<long, E3DServerResponse> m_discoveredServers = new Dictionary<long, E3DServerResponse>();

    public long ServerId { get; private set; }

    [Tooltip("Invoked when a server is found")]
    public E3DServerFoundUnityEvent onServerFound;


    #region Server

    public override void Start()
    {
        ServerId = RandomLong();

        // active transport gets initialised in awake so just make sure we set it here in Start() (after awake)
        // or just let the user assign it in the inspector
        if (transform == null)
            transport = Transport.activeTransport;

        m_scrollViewPos = Vector2.zero;

        base.Start();
    }

    protected override void ProcessClientRequest(E3DServerRequest request, IPEndPoint endpoint)
    {
        base.ProcessClientRequest(request, endpoint);
    }

    /// <summary>
    /// Process the request from the client
    /// </summary>
    /// <param name="request"></param>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    protected override E3DServerResponse ProcessRequest(E3DServerRequest request, IPEndPoint endpoint)
    {
        try
        {
            return new E3DServerResponse
            {
                uri = transport.ServerUri(),
                serverId = ServerId,
                hostname = GameCtrl.Instance.m_Hostname,
                cmdrName = GameCtrl.Instance.m_CmdrName,
                maxPlayers = GameCtrl.Instance.maxConnections,
                numPlayers = NetworkServer.connections.Count,
                difficulty = GameCtrl.Instance.m_Difficulty,
                curMapIndex = (Globals.m_CurrentMap != null) ? Array.IndexOf(GameCtrl.MapList, Globals.m_CurrentMap) : -1,
                mapName = (Globals.m_CurrentMap != null) ? Globals.m_CurrentMap.m_DisplayName : "unnamed",
                isPlaying = (GameCtrl.Instance.CurrentGameMode != null)
            };
        }
        catch (NotImplementedException)
        {
            Debug.LogError($"Transport {transport} does not support network discovery");
            throw;
        }
    }

    #endregion


    #region Client

    protected override E3DServerRequest GetRequest()
    {
        return new E3DServerRequest();
    }

    protected override void ProcessResponse(E3DServerResponse response, IPEndPoint endpoint)
    {
        // we received a message from the remote endpoint
        response.EndPoint = endpoint;

        // although we may have got a valid url, we may not be able to resolve the provided host
        // however, we know the real ip address of the server because we just received a packet from it, so use that as host
        UriBuilder realuri = new UriBuilder(response.uri)
        {
            Host = response.EndPoint.Address.ToString()
        };

        response.uri = realuri.Uri;

        onServerFound.Invoke(response);
    }

    #endregion


    [Serializable]
    public class E3DServerFoundUnityEvent : UnityEvent<E3DServerResponse> { };
}
