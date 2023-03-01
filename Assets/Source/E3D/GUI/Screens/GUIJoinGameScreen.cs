using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Net;

namespace E3D
{
    public class GUIJoinGameScreen : GUIScreen, GUIListItem.IStateChangedHandler
    {
        [Header("Join Game Screen")]
        public GameObject m_ServerItemPrefab;
        public RectTransform m_ServerListContentTransform;

        public GameObject m_ServerInfoSection;
        public Image m_ImgMap;
        public TMP_Text m_HostnameTxt;
        public TMP_Text m_CmdrnameTxt;
        public TMP_Text m_ServerStatusTxt;
        public TMP_Text m_MapNameTxt;
        public TMP_Text m_DifficultyTxt;
        public TMP_Text m_NumPlayersTxt;
        //public TMP_InputField m_IpAddressTxtbox;

        public GameObject m_DirectConnPopup;
        public GameObject m_ConnectingPopup;

        public Button m_BtnConnect;

        public string IpAddressString { get; set; }

        private bool m_connecting;
        private List<GUIListItem> m_serverListItems = new List<GUIListItem>();
        private List<long> m_serverIds = new List<long>();
        private readonly Dictionary<long, E3DServerResponse> m_discoveredServers = new Dictionary<long, E3DServerResponse>();


        public override void Open(UnityAction onFinishAnim = null)
        {
            m_connecting = false;
            Globals.m_SelectedTargetServer = null;

            m_ServerInfoSection.SetActive(false);

            GameCtrl.Instance.onLobbyPlayerEnter += OnLobbyPlayerEnter;
            GameCtrl.Instance.onClientDisconnect += OnClientDisconnect;
            GameCtrl.Instance.m_NetDiscovery.onServerFound.AddListener(OnServerFound);

            Refresh();

            base.Open(onFinishAnim);
        }

        public override void Close(UnityAction onFinishAnim = null)
        {
            GameCtrl.Instance.onLobbyPlayerEnter -= OnLobbyPlayerEnter;
            GameCtrl.Instance.onClientDisconnect -= OnClientDisconnect;
            GameCtrl.Instance.m_NetDiscovery.onServerFound.RemoveListener(OnServerFound);
            ClearServerListGUI();

            base.Close(onFinishAnim);
        }

        private void LateUpdate()
        {
            if (Globals.m_SelectedTargetServer != null)
            {
                var server = Globals.m_SelectedTargetServer;
                m_NumPlayersTxt.text = server.numPlayers.ToString() + " / " + server.maxPlayers.ToString();
                m_ServerStatusTxt.text = server.isPlaying ? "Playing" : "Waiting";
            }

            m_BtnConnect.interactable = Globals.m_SelectedTargetServer != null;
        }

        public void Refresh()
        {
            GameCtrl.Instance.m_NetDiscovery.StopDiscovery();

            ClearServerListGUI();
            m_discoveredServers.Clear();
            m_serverIds.Clear();

            GameCtrl.Instance.m_NetDiscovery.StartDiscovery();
        }

        public void Connect()
        {
            if (Globals.m_SelectedTargetServer == null)
            {
                var alert = PopupboxFactory.Instance.Create<GUIPopupAlert>(m_RectTransform);
                alert.m_RectTransform.SetAsLastSibling();
                alert.Open("ERROR: Server does not exist. Please click on Refresh below.");

                return;
            }

            if (Globals.m_SelectedTargetServer.isPlaying)
            {
                var alert = PopupboxFactory.Instance.Create<GUIPopupAlert>(m_RectTransform);
                alert.m_RectTransform.SetAsLastSibling();
                alert.Open("The game is in session, you cannot join this server.");

                return;
            }

            Globals.m_ConnectGameType = 2;

            if (GUIController.Instance.ActiveScreen == this)
                GUIController.Instance.CloseCurrentScreen();

            GameCtrl.Instance.JoinLobby(Globals.m_SelectedTargetServer.uri);

            //// set current map
            //int curMapIndex = Globals.m_SelectedTargetServer.curMapIndex;
            //Globals.m_CurrentMap = GameCtrl.MapList[curMapIndex];

            //string loadMapScene = Globals.m_CurrentMap.m_SceneFileName;
            ////string loadMapScene = Globals.m_SelectedTargetServer.mapName;

            //ScreenWiper.Instance.DoFade(ScreenWiper.FillMode.Fill, 1.0f, 0.0f, () =>
            //{
            //    if (GUIController.Instance.ActiveScreen == this)
            //        GUIController.Instance.CloseCurrentScreen();

            //    SceneLoader.Instance.LoadScene(loadMapScene);
            //});
        }

        public void DirectConnect()
        {
            if (string.IsNullOrEmpty(IpAddressString))
            {
                var addrEmptyAlert = PopupboxFactory.Instance.Create<GUIPopupAlert>(m_RectTransform);
                addrEmptyAlert.m_RectTransform.SetAsLastSibling();
                addrEmptyAlert.Open("IP address not entered.");

                return;
            }

            // check format of IP address
            // make sure its Ipv4
            if (IPAddress.TryParse(IpAddressString, out IPAddress address))
            {
                if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    goto not_ipv4;
                }

                // connect to the server
                GameCtrl.Instance.networkAddress = address.ToString();
                GameCtrl.Instance.StartClient();
                m_connecting = true;

                // open a dialog box to show that it is connecting...
                m_ConnectingPopup.SetActive(true);
            }
            else
            {
                goto not_ipv4;
            }

            return;

        not_ipv4:
            var alert = PopupboxFactory.Instance.Create<GUIPopupAlert>(m_RectTransform);
            alert.m_RectTransform.SetAsLastSibling();
            alert.Open("IP address format is wrong. Make sure IP address is in IPv4 format.");
        }

        public void OnServerFound(E3DServerResponse info)
        {
            // NOTE: you can check the versioning to decide if you can connect to the server or not using this method
            if (m_discoveredServers.ContainsKey(info.serverId) && m_serverIds.Contains(info.serverId))
                return;

            GameObject newServerListItemObject = Instantiate(m_ServerItemPrefab);
            GUIListItem newServerListItem = newServerListItemObject.GetComponent<GUIListItem>();

            m_serverIds.Add(info.serverId);
            m_discoveredServers[info.serverId] = info;
            m_serverListItems.Add(newServerListItem);

            newServerListItemObject.SetActive(true);

            newServerListItem.Owner = this.gameObject;
            newServerListItem.LabelString = info.hostname;
            newServerListItem.name = "Item_" + info.hostname;
            newServerListItem.m_RectTransform.SetParent(m_ServerListContentTransform, false);
        }

        private void ClearServerListGUI()
        {
            for (int i = 0; i < m_serverListItems.Count; i++)
            {
                var cur = m_serverListItems[i];
                Destroy(cur.gameObject);
            }

            m_serverListItems.Clear();
        }

        public void ListItem_StateChanged(GUIListItem item, bool state)
        {
            // find the server id from list item
            int idx = m_serverListItems.IndexOf(item);
            long selectedSvId = m_serverIds[idx];

            Globals.m_SelectedTargetServer = m_discoveredServers[selectedSvId];
            m_ServerInfoSection.SetActive(true);

            m_ImgMap.sprite = GameCtrl.MapList[Globals.m_SelectedTargetServer.curMapIndex].m_Thumbnail;
            m_HostnameTxt.text = Globals.m_SelectedTargetServer.hostname;
            m_CmdrnameTxt.text = Globals.m_SelectedTargetServer.cmdrName;
            m_MapNameTxt.text = Globals.m_SelectedTargetServer.mapName;
            m_DifficultyTxt.text = Globals.m_SelectedTargetServer.difficulty.ToString();
        }

        private void OnClientDisconnect()
        {
            if (m_connecting)
            {
                m_ConnectingPopup.SetActive(false);

                var alert = PopupboxFactory.Instance.Create<GUIPopupAlert>(m_RectTransform);
                alert.m_RectTransform.SetAsLastSibling();
                alert.Open("Connection error. The address " + GameCtrl.Instance.networkAddress.ToString() + " does not exist.");
                
                m_connecting = false;
            }
        }

        private void OnLobbyPlayerEnter()
        {
            m_connecting = false;

            m_ConnectingPopup.SetActive(false);

            if (GUIController.Instance.ActiveScreen == this)
                GUIController.Instance.CloseCurrentScreen();
        }

        protected override void OnDestroy()
        {
            if (GameCtrl.Instance != null)
            {
                GameCtrl.Instance.onLobbyPlayerEnter -= OnLobbyPlayerEnter;
                GameCtrl.Instance.onClientDisconnect -= OnClientDisconnect;
                GameCtrl.Instance.m_NetDiscovery.onServerFound.RemoveListener(OnServerFound);
            }
            
            base.OnDestroy();
        }
    }
}
