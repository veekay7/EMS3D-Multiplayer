using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System;

public class LobbyManager : SingletonBehaviour<LobbyManager>
{
    private const int MAX_PLAYERS = 64;

    public GameObject m_PlayerItemPrefab;
    public RectTransform m_PlayerListContent;

    public TMP_Text m_HostnameTxt;
    public TMP_Text m_CmdrNameTxt;
    public TMP_Text m_MapNameTxt;
    public TMP_Text m_DifficultyTxt;

    public TMP_Text m_SelectRoleLabelTxt;
    public TMP_Dropdown m_SelectRoleCmb;
    public Button m_BtnStart;
    public Button m_BtnReady;
    public Button m_BtnNotReady;

    private RectTransform m_RectTransform;
    private List<GUILobbyPlayerListItem> m_roomPlayerItemPool = new List<GUILobbyPlayerListItem>();


    protected override void AfterAwake()
    {
        base.AfterAwake();
        m_RectTransform = GetComponent<RectTransform>();
    }

    public void Start()
    {
        // register events first
        GameCtrl.Instance.onLobbyPlayerEnter += PopulateLobbyPlayerList;
        GameCtrl.Instance.onLobbyPlayerEnter += UpdateLocalLobbyPlayer;
        GameCtrl.Instance.onLobbyPlayerExit += PopulateLobbyPlayerList;
        E3DLobbyPlayer.onReadyStateChanged += Callback_LobbyPlayerReadyStateChanged;
        E3DLobbyPlayer.onReceiveLobbyMsg += Callback_LobbyMsgReceived;

        NetworkManagerMode netMode = GameCtrl.Instance.mode;

        // create pool of lobby player list items
        // server supported max players is 64. Max number of players cannot exceed 64 (hard limit)!!
        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            GameObject newLobbyPlayerItemObject = Instantiate(m_PlayerItemPrefab);
            GUILobbyPlayerListItem newLobbyPlayerItem = newLobbyPlayerItemObject.GetComponent<GUILobbyPlayerListItem>();

            newLobbyPlayerItem.m_RectTransform.SetParent(m_PlayerListContent, false);

            m_roomPlayerItemPool.Add(newLobbyPlayerItem);
        }

        // deal with them buttons
        m_BtnStart.gameObject.SetActive((netMode == NetworkManagerMode.ServerOnly || netMode == NetworkManagerMode.Host));
        m_BtnStart.interactable = netMode != NetworkManagerMode.Offline;

        m_BtnReady.gameObject.SetActive(true);
        m_BtnNotReady.gameObject.SetActive(false);

        // select role combo box can only appear 
        m_SelectRoleLabelTxt.gameObject.SetActive(netMode == NetworkManagerMode.ClientOnly);
        m_SelectRoleCmb.gameObject.SetActive(netMode == NetworkManagerMode.ClientOnly);

        // set the name of the commander of this mission lobby
        // if host, just show player name set in GameCtrl, else, use the name set in Globals.m_SelectedTargetServer
        if (netMode == NetworkManagerMode.Host)
        {
            m_HostnameTxt.text = GameCtrl.Instance.m_Hostname;
            m_CmdrNameTxt.text = GameCtrl.Instance.m_PlayerName;
            m_MapNameTxt.text = Globals.m_CurrentMap.m_DisplayName;
            m_DifficultyTxt.text = GameCtrl.Instance.m_Difficulty.ToString();
        }
        else if (netMode == NetworkManagerMode.ClientOnly)
        {
            // set the index for the select role combo box first
            // if the selected role is either a incident commander or spectator, change it to default to triage officer
            if (GameCtrl.Instance.m_SelectedRole == EEmtRole.Spectator || GameCtrl.Instance.m_SelectedRole == EEmtRole.IncidentCmdr)
                GameCtrl.Instance.m_SelectedRole = EEmtRole.TriageOffr;

            // something somewhere here is fucked!!
            int value = (int)GameCtrl.Instance.m_SelectedRole - 2;
            value = Mathf.Clamp(value, 0, 2);
            m_SelectRoleCmb.value = value;

            if (Globals.m_SelectedTargetServer != null)
            {
                m_HostnameTxt.text = Globals.m_SelectedTargetServer.hostname;
                m_CmdrNameTxt.text = Globals.m_SelectedTargetServer.cmdrName;
                m_MapNameTxt.text = Globals.m_SelectedTargetServer.mapName;
                m_DifficultyTxt.text = Globals.m_SelectedTargetServer.difficulty.ToString();
            }
        }
    }

    private void Callback_LobbyMsgReceived(E3DLobbyPlayer arg0, string reason)
    {
        var alert = PopupboxFactory.Instance.Create<GUIPopupAlert>(m_RectTransform.GetComponent<RectTransform>());
        alert.m_RectTransform.SetAsLastSibling();
        alert.Open(reason);
    }

    public void SetReady(bool ready)
    {
        if (E3DLobbyPlayer.Local != null)
        {
            m_SelectRoleCmb.interactable = !ready;
            E3DLobbyPlayer.Local.CmdChangeReadyState(ready);
        }
    }

    public void StartGame()
    {
        if (GameCtrl.Instance.allPlayersReady)
        {
            //Debug.Log("Start the game you twat!!");
            GameCtrl.Instance.StartGame();
        }
        else
        {
            var alert = PopupboxFactory.Instance.Create<GUIPopupAlert>(m_RectTransform.GetComponent<RectTransform>());
            alert.m_RectTransform.SetAsLastSibling();
            alert.Open("Game can only start when all players are ready.");
        }
    }

    public void RoleSelectCmb_ValueChanged(int index)
    {
        if (E3DLobbyPlayer.Local != null)
        {
            GameCtrl.Instance.m_SelectedRole = ((EEmtRole)index + 2);
            E3DLobbyPlayer.Local.CMD_SetSelectedRole((EEmtRole)index + 2);
        }
    }

    public void LeaveLobby()
    {
        GameCtrl.Instance.Disconnect();
    }

    private void PopulateLobbyPlayerList()
    {
        ClearLobbyPlayerItems();

        var roomSlots = GameCtrl.Instance.roomSlots.ToArray();
        for (int roomSlotIdx = 0; roomSlotIdx < roomSlots.Length; roomSlotIdx++)
        {
            for (int playerItemIdx = 0; playerItemIdx < MAX_PLAYERS; playerItemIdx++)
            {
                if (m_roomPlayerItemPool[playerItemIdx].LobbyPlayer == null)
                {
                    m_roomPlayerItemPool[playerItemIdx].LobbyPlayer = roomSlots[roomSlotIdx] as E3DLobbyPlayer;
                    m_roomPlayerItemPool[playerItemIdx].gameObject.SetActive(true);
                    break;
                }
            }
        }
    }

    private void UpdateLocalLobbyPlayer()
    {
        if (E3DLobbyPlayer.Local != null)
        {
            m_BtnReady.gameObject.SetActive(!E3DLobbyPlayer.Local.readyToBegin);
            m_BtnNotReady.gameObject.SetActive(E3DLobbyPlayer.Local.readyToBegin);

            //// from the select role combo, set the selected role
            //if (GameCtrl.Instance.mode == NetworkManagerMode.ClientOnly)
            //{
            //    if (E3DLobbyPlayer.Local != null)
            //    {
            //        EEmtRole role = (EEmtRole)m_SelectRoleCmb.value + 2;
            //        E3DLobbyPlayer.Local.CMD_SetSelectedRole(role);
            //    }
            //}
        }
    }

    private void ClearLobbyPlayerItems()
    {
        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            m_roomPlayerItemPool[i].LobbyPlayer = null;
            m_roomPlayerItemPool[i].gameObject.SetActive(false);
        }
    }

    private void Callback_LobbyPlayerReadyStateChanged(E3DLobbyPlayer lobbyPlayer, bool oldReadyState, bool newReadyState)
    {
        if (lobbyPlayer == E3DLobbyPlayer.Local)
        {
            m_BtnReady.gameObject.SetActive(!newReadyState);
            m_BtnNotReady.gameObject.SetActive(newReadyState);
        }
    }

    protected override void OnDestroy()
    {
        foreach (var item in m_roomPlayerItemPool.ToArray())
        {
            Destroy(item.gameObject);
            m_roomPlayerItemPool.Remove(item);
        }
        m_roomPlayerItemPool.Clear();

        if (GameCtrl.Instance != null)
        {
            E3DLobbyPlayer.onReadyStateChanged -= Callback_LobbyPlayerReadyStateChanged;
            GameCtrl.Instance.onLobbyPlayerExit -= PopulateLobbyPlayerList;
            GameCtrl.Instance.onLobbyPlayerEnter -= UpdateLocalLobbyPlayer;
            GameCtrl.Instance.onLobbyPlayerEnter -= PopulateLobbyPlayerList;
        }

        base.OnDestroy();
    }
}
