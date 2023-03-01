using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace E3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))]
    public class PlayerUIBase : MonoBehaviour
    {
        public readonly static string[] GameStateMsgs = { "Waiting for Commander to Start",
            "Game Not Started",
            "Game Started",
        };

        [HideInInspector]
        public RectTransform m_RectTransform;
        [HideInInspector]
        public Canvas m_Canvas;

        [Header("Player UI Base")]
        public GUIGameStateDisplay m_GameState;
        public List<HudBaseView> m_CachedViews = new List<HudBaseView>();
        public HudBaseView m_StartView;

        [Header("Game Messages")]
        public GameObject m_GameStateMsgContent;
        public TMP_Text m_GameStateMsgTxt;

        protected EMatchState m_lastMatchState;
        protected HudBaseView m_curView;
        

        public E3DPlayer Player { get; private set; }

        public HudBaseView ActiveView { get => m_curView; }


        protected virtual void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_Canvas = GetComponent<Canvas>();

            m_GameStateMsgContent.SetActive(false);

            m_curView = null;
        }

        protected virtual void Reset() { return; }

        protected virtual void Start()
        {
            for (int i = 0; i < m_CachedViews.Count; i++)
            {
                m_CachedViews[i].Owner = this;
            }

            if (m_StartView != null)
                OpenView(m_StartView);
        }

        protected virtual void Update() { }

        protected virtual void LateUpdate()
        {
            if (GameCtrl.Instance != null && GameState.Current != null)
            {
                var curMatchState = GameState.Current.m_CurrentMatchState;

                if (m_lastMatchState == EMatchState.Enter && curMatchState == EMatchState.WaitingToStart)
                {
                    m_GameStateMsgContent.SetActive(true);

                    if (GameCtrl.Instance.mode == NetworkManagerMode.Host)
                    {
                        m_GameStateMsgTxt.text = GameStateMsgs[1];
                    }
                    else if (GameCtrl.Instance.mode == NetworkManagerMode.ClientOnly)
                    {
                        m_GameStateMsgTxt.text = GameStateMsgs[0];
                    }
                }
                else if (m_lastMatchState == EMatchState.WaitingToStart && curMatchState == EMatchState.InProgress)
                {
                    StartCoroutine(Co_ShowTimedGameStateMsg(GameStateMsgs[2], 1.5f));
                }

                m_lastMatchState = curMatchState;
            }
        }

        private IEnumerator Co_ShowTimedGameStateMsg(string msg, float duration)
        {
            m_GameStateMsgContent.gameObject.SetActive(true);
            m_GameStateMsgTxt.text = msg;

            yield return new WaitForSeconds(duration);

            m_GameStateMsgContent.gameObject.SetActive(false);
        }

        public virtual void SetPlayer(E3DPlayer newPlayer)
        {
            Player = newPlayer;
            if (m_GameState != null)
                m_GameState.Player = Player;
        }

        public virtual void UnsetPlayer()
        {
            Player = null;
        }

        public void SetVisible(bool value)
        {
            m_Canvas.enabled = value;
        }

        public void OpenView(int index)
        {
            if (m_CachedViews.Count > 0 && index >= 0 && index <= m_CachedViews.Count - 1)
            {
                OpenView(m_CachedViews[index]);
            }
        }

        public virtual void OpenView(HudBaseView newView)
        {
            if (m_curView == newView)
                return;

            newView.gameObject.SetActive(true);

            newView.transform.SetAsLastSibling();

            // close the old panel
            CloseCurrentView();

            m_curView = newView;

            // open the new panel
            m_curView.Open();
        }

        public virtual void CloseCurrentView()
        {
            if (m_curView == null)
                return;

            m_curView.Close();

            m_curView.gameObject.SetActive(false);

            m_curView = null;
        }

        public void PauseGame()
        {
            if (Player == null)
                return;

            Player.Pause();
        }

        public void OpenBriefingScreen()
        {
            if (Player == null)
                return;

            Player.OpenBriefingScreen();
        }

        public void ShowTextBoxPrompt(string outputString, UnityAction dlgBoxResponseFunc = null)
        {
            if (Player != null)
            {
                Player.IsWaitingForPrompt = true;

                var box = PopupboxFactory.Instance.Create<GUIDialogueBox01>(m_RectTransform);
                
                box.m_RectTransform.SetAsLastSibling();

                box.Open(outputString, () => {

                    if (dlgBoxResponseFunc != null)
                        dlgBoxResponseFunc.Invoke();

                    if (Player != null)
                        Player.IsWaitingForPrompt = false;
                });
            }
        }

        protected virtual void OnDestroy()
        {
        }

        // editor only
        protected virtual void OnValidate()
        {
            m_RectTransform = gameObject.GetOrAddComponent<RectTransform>();
            m_Canvas = gameObject.GetOrAddComponent<Canvas>();
        }
    }
}
