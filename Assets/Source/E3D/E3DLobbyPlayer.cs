using Mirror;
using UnityEngine.Events;

public class E3DLobbyPlayer : NetworkRoomPlayer
{
    [SyncVar]
    public string m_PlayerName = "unnamed";
    [SyncVar]
    public EEmtRole m_SelectedRole = EEmtRole.Spectator;

    public static event UnityAction<E3DLobbyPlayer, bool, bool> onReadyStateChanged;
    public static event UnityAction<E3DLobbyPlayer, string> onReceiveLobbyMsg;

    public static E3DLobbyPlayer Local { get; private set; }


    public override void OnStartLocalPlayer()
    {
        if (Local == null)
            Local = this;

        CMD_SetPlayerName(GameCtrl.Instance.m_PlayerName);

        if (GameCtrl.Instance.mode == NetworkManagerMode.Host)
        {
            // you are definitely an incident commander if you are the host
            CMD_SetSelectedRole(EEmtRole.IncidentCmdr);
        }
        else if (GameCtrl.Instance.mode == NetworkManagerMode.ClientOnly)
        {
            // default to whatever was previously selected by the combo box
            CMD_SetSelectedRole(GameCtrl.Instance.m_SelectedRole);
        }
    }

    [Command(requiresAuthority = false)]
    public void CMD_InformGameCannotStart(string reason)
    {
        RPC_InformGameCannotStart(reason);
    }

    [TargetRpc]
    private void RPC_InformGameCannotStart(string reason)
    {
        if (onReceiveLobbyMsg != null)
            onReceiveLobbyMsg(this, reason);
    }

    [Command]
    public void CMD_SetPlayerName(string newPlayerName)
    {
        m_PlayerName = newPlayerName;
    }

    [Command]
    public void CMD_SetSelectedRole(EEmtRole emtType)
    {
        m_SelectedRole = emtType;
        RPC_UpdateSelectedRoleOnGameCtrl(m_SelectedRole);
    }

    [TargetRpc]
    private void RPC_UpdateSelectedRoleOnGameCtrl(EEmtRole emtType)
    {
        if (isLocalPlayer)
            GameCtrl.Instance.m_SelectedRole = emtType;
    }

    /// <summary>
    /// This is a hook that is invoked on clients when a RoomPlayer switches between ready or not ready.
    /// <para>This function is called when the a client player calls SendReadyToBeginMessage() or SendNotReadyToBeginMessage().</para>
    /// </summary>
    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        if (isLocalPlayer)
        {
            if (onReadyStateChanged != null)
                onReadyStateChanged(this, oldReadyState, newReadyState);
        }
    }
}
