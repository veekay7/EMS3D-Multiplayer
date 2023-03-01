using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace E3D
{
    public class IncidentCmdrUI : PlayerUIBase
    {
        [Header("Incident Commander UI", order = 1)]
        [Header("Incident Budget Display", order = 2)]
        public Slider m_IncidentBudgetSlider;
        public Image m_IncidentBudgetSliderFill;
        public TMP_Text m_IncidentBudgetStatusTxt;
        public TMP_Text m_GivenMoneyTxt;
        public TMP_Text m_UsedMoneyTxt;
        public Color m_LowBudgetSliderColor;
        public Color m_MidBudgetSliderColor;
        public Color m_HiBudgetSliderColor;
        public Color m_LowBudgetTxtColor;
        public Color m_MidBudgetTxtColor;
        public Color m_HiBudgetTxtColor;
        public GUISelectedLocation m_SelectedLocationUI;
        public RectTransform m_UIMainRectTransform;

        [Header("Buttons")]
        public Button m_StartGameBtn;
        public Button m_HospitalsBtn;
        public Button m_AmbulanceDepotsBtn;
        public Button m_AddFAPBtn;
        public Button m_AddEvacPointBtn;

        [Header("Windows")]
        public GUIRefillItemWindow m_RefillItemWnd;
        public GUIHospitalWindow m_HospitalWnd;
        public GUIAmbulanceDepotWindow m_AmbulanceDepotWnd;
        public GUIPlayerListWindow m_PlayerListWnd;

        private E3DIncidentCmdrPlayer m_incidentCmdr;
        private AVictimPlaceableArea m_selectedArea;

        public bool InStartView { get; private set; }


        protected override void Awake()
        {
            base.Awake();

            m_StartGameBtn.interactable = true;
            m_HospitalsBtn.interactable = false;
            m_AmbulanceDepotsBtn.interactable = false;
            m_AddFAPBtn.interactable = false;
            m_AddEvacPointBtn.interactable = false;
            m_AmbulanceDepotWnd.onOkBtnClickedFunc += SendAmbulanceToEvacPoint;
        }

        protected override void Start()
        {
            if (GameMode.Current != null && GameState.Current != null)
            {
                m_IncidentBudgetSlider.minValue = 0;
                m_IncidentBudgetSlider.maxValue = 1.0f;
                m_GivenMoneyTxt.text = GameMode.Current.m_MoneyGiven.ToString();
            }

            base.Start();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (GameMode.Current != null && GameState.Current != null)
            {
                // incident budget slider stuff
                float pcBudget = (GameMode.Current.m_MoneyUsed / (float)GameMode.Current.m_MoneyGiven);
                m_IncidentBudgetSlider.value = pcBudget;

                float icBudgetValue = m_IncidentBudgetSlider.value;
                if (icBudgetValue >= 0.0f && icBudgetValue < 0.3f)
                {
                    m_IncidentBudgetSliderFill.color = m_LowBudgetSliderColor;
                }
                else if (icBudgetValue >= 0.3f && icBudgetValue < 0.6f)
                {
                    m_IncidentBudgetSliderFill.color = m_MidBudgetSliderColor;
                }
                else
                {
                    m_IncidentBudgetSliderFill.color = m_HiBudgetSliderColor;
                }

                // update the incident budget status
                m_UsedMoneyTxt.text = GameMode.Current.m_MoneyUsed.ToString();
                m_IncidentBudgetStatusTxt.text = (Mathf.Ceil(pcBudget * 100.0f)).ToString() + " %";
            }
        }

        public override void SetPlayer(E3DPlayer newPlayer)
        {
            base.SetPlayer(newPlayer);
            if (newPlayer != null)
            {
                m_incidentCmdr = (E3DIncidentCmdrPlayer)newPlayer;
                m_incidentCmdr.onResponseRecvFunc.AddListener(Callback_ResponseReceived);
                m_RefillItemWnd.IncidentCmdr = m_incidentCmdr;
            }
            else
            {
                m_RefillItemWnd.IncidentCmdr = null;
            }
        }

        public void StartGame()
        {
            if (m_incidentCmdr == null)
                return;
            m_incidentCmdr.StartGame();
            m_StartGameBtn.interactable = false;
            m_HospitalsBtn.interactable = true;
            m_AmbulanceDepotsBtn.interactable = true;
            m_AddFAPBtn.interactable = true;
            m_AddEvacPointBtn.interactable = true;
        }

        public void OnAreaSelected(AVictimPlaceableArea area)
        {
            OpenView(1);
            m_SelectedLocationUI.Open(area);
            m_selectedArea = area;
        }

        public void OnAreaDeselected()
        {
            if (m_curView != null && m_curView == m_CachedViews[1])
            {
                CloseCurrentView();
                OpenView(m_CachedViews[0]);
                m_selectedArea = null;
            }
        }

        public void SpawnFAP()
        {
            if (m_incidentCmdr == null)
                return;

            m_incidentCmdr.StartBuildMode(GameCtrl.Instance.FindPrefab("ALocPoint_FirstAid"));
        }

        public void SpawnEvacPoint()
        {
            if (m_incidentCmdr == null)
                return;

            m_incidentCmdr.StartBuildMode(GameCtrl.Instance.FindPrefab("ALocPoint_Evac"));
        }

        public void OpenLocationRenameWindow()
        {
            if (m_incidentCmdr == null || m_selectedArea == null)
                return;

            m_incidentCmdr.IsWaitingForPrompt = true;

            var renameDlg = PopupboxFactory.Instance.Create<GUIPopupRenameLocation>(m_UIMainRectTransform);
            renameDlg.m_RectTransform.SetAsLastSibling();
            renameDlg.Open(m_selectedArea, null, null, (location, response) => {
                m_incidentCmdr.IsWaitingForPrompt = false;
            });
        }

        public void AddPlayerToLocation()
        {
            if (m_incidentCmdr == null || m_selectedArea == null)
                return;

            m_incidentCmdr.IsWaitingForPrompt = true;

            EEmtRole windowMode = EEmtRole.TriageOffr;
            if (m_selectedArea is AEvacPoint)
                windowMode = EEmtRole.EvacOffr;
            else if (m_selectedArea is AFirstAidPoint)
                windowMode = EEmtRole.FirstAidPointDoc;

            var box = PopupboxFactory.Instance.Create<GUIPopupAddBot>(m_UIMainRectTransform);
            box.m_RectTransform.SetAsLastSibling();

            box.Open(windowMode, null, null, (response, role, playerName) =>
            {
                if (response == 0)
                {
                    E3DEmtPlayerBase newCpuPlayer = null;
                    switch (role)
                    {
                        case EEmtRole.TriageOffr:
                            newCpuPlayer = GameCtrl.Instance.SV_Spawn("AE3DCpu_TriageOffr").GetComponent<E3DEmtPlayerBase>();
                            break;

                        case EEmtRole.FirstAidPointDoc:
                            newCpuPlayer = GameCtrl.Instance.SV_Spawn("AE3DCpu_FirstAidDoc").GetComponent<E3DEmtPlayerBase>();
                            break;

                        case EEmtRole.EvacOffr:
                            newCpuPlayer = GameCtrl.Instance.SV_Spawn("AE3DCpu_EvacOffr").GetComponent<E3DEmtPlayerBase>();
                            break;
                    }

                    newCpuPlayer.CMD_SetPlayerName("[BOT] " + playerName);
                    newCpuPlayer.CMD_EnableInput(true);
                    newCpuPlayer.SetLocation(m_selectedArea);

                    var dlgbox = PopupboxFactory.Instance.Create<GUIDialogueBox01>(m_UIMainRectTransform);
                    dlgbox.m_RectTransform.SetAsLastSibling();
                    dlgbox.Open("Added " + newCpuPlayer.m_PlayerName + " to " + m_selectedArea.m_PrintName + ". ", () =>
                    {
                        m_incidentCmdr.IsWaitingForPrompt = false;
                    });
                }
                else if (response == 1)
                {
                    m_incidentCmdr.IsWaitingForPrompt = false;
                }
            });
        }

        public void OpenPlayerListWindow()
        {
            if (m_incidentCmdr == null || m_selectedArea == null)
                return;

            m_PlayerListWnd.m_RectTransform.SetAsLastSibling();
            m_PlayerListWnd.m_Location = m_selectedArea;
            m_PlayerListWnd.m_Players = m_selectedArea.GetPlayers();
            m_PlayerListWnd.gameObject.SetActive(true);
        }

        public void OpenRefillWindow()
        {
            m_RefillItemWnd.m_RectTransform.SetAsLastSibling();
            m_RefillItemWnd.FirstAidPoint = (AFirstAidPoint)m_selectedArea;
            m_RefillItemWnd.gameObject.SetActive(true);
        }

        public void RefillItems()
        {
            m_RefillItemWnd.Refill();
        }

        public void OpenHospitalWindow()
        {
            m_HospitalWnd.Mode = GUIHospitalWindow.WindowMode.CheckMode;
            m_HospitalWnd.m_RectTransform.SetAsLastSibling();
            m_HospitalWnd.gameObject.SetActive(true);
        }

        [EnumAction(typeof(GUIAmbulanceDepotWindow.WindowMode))]
        public void OpenAmbulanceDepotWindow(int mode)
        {
            m_AmbulanceDepotWnd.Mode = (GUIAmbulanceDepotWindow.WindowMode)mode;
            m_AmbulanceDepotWnd.gameObject.SetActive(true);
            m_AmbulanceDepotWnd.m_RectTransform.SetAsLastSibling();
        }

        public override void OpenView(HudBaseView newView)
        {
            base.OpenView(newView);

            if (m_curView != null && m_curView == m_CachedViews[0])
                InStartView = true;
        }

        private void SendAmbulanceToEvacPoint(AAmbulanceDepot arg0)
        {
            if (m_incidentCmdr == null)
                return;

            if (arg0 != null && m_selectedArea != null && m_selectedArea is AEvacPoint)
            {
                m_incidentCmdr.SendAmbulanceToEvacPoint(arg0, (AEvacPoint)m_selectedArea);
            }
        }

        private void Callback_ResponseReceived(ActorNetResponse_s arg0)
        {
            if (arg0.m_ResponseType == "send_ambulance")
            {
                m_incidentCmdr.IsWaitingForPrompt = true;

                var box = PopupboxFactory.Instance.Create<GUIDialogueBox01>(m_RectTransform);
                box.m_RectTransform.SetAsLastSibling();
                box.Open(arg0.m_Message, () => {
                    m_incidentCmdr.IsWaitingForPrompt = false;
                });
            }
        }
    }
}
