using Mirror;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

namespace E3D
{
    public class GUIPreMissionScreen : GUIScreen
    {
        [Header("Pre-Mission Screen")]
        public CanvasGroup m_StartSubScreen;
        public CanvasGroup m_PlanningSubScreenRef;     // reference to the planning screen

        [Header("Panel - Breifing")]
        public CanvasGroup m_BriefingScreenRef;
        public TMP_Text m_ObjectivesTxt;
        public VideoPlayer m_VideoPlayer;
        public RenderTexture m_VideoRT;

        [Header("Panel - Roster")]
        public GUIConnPlayerItem m_ConnItemPrefab;
        public RectTransform m_RosterItemsContent;

        private CanvasGroup m_activeScreen;
        private List<GUIConnPlayerItem> m_rosterConnItems = new List<GUIConnPlayerItem>();


        public override void Open(UnityAction onFinishAnim = null)
        {
            //GameCtrl.Instance.onServerAddPlayer += OnNewPlayerJoined;
            //GameCtrl.Instance.onClientDisconnect += OnClientDisconnected;

            if (m_StartSubScreen != null)
                OpenSubScreen(m_StartSubScreen);

            base.Open(onFinishAnim);
        }

        public override void Close(UnityAction onFinishAnim = null)
        {
            //GameCtrl.Instance.onServerAddPlayer -= OnNewPlayerJoined;
            //GameCtrl.Instance.onClientDisconnect -= OnClientDisconnected;

            base.Close(onFinishAnim);
        }

        private void OnNewPlayerJoined(NetworkConnection conn)
        {
            if (conn.identity != null)
            {
                var newItem = Instantiate(m_ConnItemPrefab);
                m_rosterConnItems.Add(newItem);

                newItem.gameObject.SetActive(true);
                newItem.SetPlayer(conn);
                newItem.m_RectTransform.SetParent(m_RosterItemsContent, false);
            }
        }

        private void OnClientDisconnected(NetworkConnection conn)
        {
            if (conn.identity != null)
            {
                for (int i = 0; i < m_rosterConnItems.Count; i++)
                {
                    if (m_rosterConnItems[i].NetConnection == conn)
                    {
                        Destroy(m_rosterConnItems[i].gameObject);
                        return;
                    }
                }
            }
        }

        public void OpenSubScreen(CanvasGroup newScreen)
        {
            if (m_activeScreen != null)
            {
                CloseActiveSubScreen();
            }

            m_activeScreen = null;

            m_activeScreen = newScreen;

            newScreen.alpha = 1.0f;
            newScreen.interactable = true;
            newScreen.blocksRaycasts = true;

            newScreen.gameObject.SetActive(true);

            if (newScreen == m_PlanningSubScreenRef)
            {
                m_BackgroundGroup.gameObject.SetActive(false);
            }
            else if (newScreen == m_BriefingScreenRef)
            {
                m_ObjectivesTxt.text = Globals.m_CurrentMap?.m_ObjectivesDesc?.text;

                m_VideoPlayer.clip = Globals.m_CurrentMap?.m_SceneVideoClip;
                m_VideoPlayer.Play();
            }
        }

        public void CloseActiveSubScreen()
        {
            if (m_activeScreen == null)
                return;

            m_activeScreen.alpha = 0.0f;
            m_activeScreen.interactable = false;
            m_activeScreen.blocksRaycasts = false;

            m_activeScreen.gameObject.SetActive(false);

            if (m_activeScreen == m_PlanningSubScreenRef)
            {
                m_BackgroundGroup.gameObject.SetActive(true);
            }
            else if (m_activeScreen == m_BriefingScreenRef)
            {
                // stop video player and release render texture if closing the briefing panel
                m_VideoPlayer.Stop();
                m_VideoRT.Release();
            }

            m_activeScreen = null;
        }

        public void StartGame()
        {
            //GameCtrl.Instance.HostRequestStartGame();

            if (GUIController.Instance.ActiveScreen == this)
                GUIController.Instance.CloseCurrentScreen();
        }
    }
}
