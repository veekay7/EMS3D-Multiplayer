using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace E3D
{
    public class EvacOffrUI : PlayerUIBase, GUIAmbulanceListButton.IClickedHandler
    {
        [Header("Ambulance List")]
        [Header("Evacuation Officer HUD")]
        public GUIAmbulanceListButton m_AmbulanceListButtonPrefab;
        public RectTransform m_AmbulanceListButtonContentTransform;
        public GameObject m_AmbulanceListGroup;
        public GameObject m_AmbulanceListBlocker;

        [Header("Ambulance Details - Victim")]
        public GameObject   m_AmbulanceDetailsGroup;
        public Button       m_BtnEvacuate;
        public Image        m_ImgVictimPortrait;
        public Image        m_ImgVictimSex;
        public TMP_Text     m_TxtVictimName;
        public Slider       m_VictimHealthbar;

        [Header("Ambulance Details - Hospital")]
        public TMP_Text m_TxtHospitalName;
        public TMP_Text m_TxtTimeToDest;
        public TMP_Text m_TxtDistToDest;
        public Button m_BtnLoadVictim;
        public Button m_BtnUnloadVictim;

        [Header("Sub Windows")]
        public GUISelectVictimWindow m_SelectVictimWindow;
        public GUIHospitalWindow m_HospitalSelectWindow;

        [Space]

        public GameObject m_ActionButtonsGroup;
        public Sprite m_NullMaleSprite;
        public Sprite m_NullFemaleSprite;
        public Sprite m_MaleSexSprite;
        public Sprite m_FemaleSexSprite;

        private E3DEvacOffrPlayer m_evacOffr;
        private AAmbulance m_selAmbulance;
        private List<GUIAmbulanceListButton> m_ambulanceButtons = new List<GUIAmbulanceListButton>();


        public E3DEvacOffrPlayer PossessedEmt { get => m_evacOffr; }

        protected override void Awake()
        {
            base.Awake();

            m_SelectVictimWindow.onCancelBtnClickedFunc += M_SelectVictimWindow_onCancelBtnClickedFunc;
            m_SelectVictimWindow.onOkBtnClickedFunc += M_SelectVictimWindow_onOkBtnClickedFunc;

            m_HospitalSelectWindow.Mode = GUIHospitalWindow.WindowMode.Usable;
            m_HospitalSelectWindow.onCancelBtnClickedFunc += M_HospitalSelectWindow_onCancelBtnClickedFunc;
            m_HospitalSelectWindow.onOkBtnClickedFunc += M_HospitalSelectWindow_onOkBtnClickedFunc;
        }

        protected override void Start()
        {
            base.Start();

            m_AmbulanceListBlocker.SetActive(false);
        }

        public override void SetPlayer(E3DPlayer newPlayer)
        {
            base.SetPlayer(newPlayer);

            if (m_evacOffr != null)
            {
                m_evacOffr.onLocEnterExitFunc.RemoveListener(Callback_AreaEnterExit);
                m_evacOffr.onLocVictimNumChangedFunc.RemoveListener(Callback_AreaVictimChanged);
                m_evacOffr.onResponseRecvFunc.RemoveListener(Callback_ResponseReceived);
                m_evacOffr.onAreaAmbulanceNumUpdateFunc -= EvacPoint_AmbulanceNumUpdated;

                m_evacOffr = null;
            }

            if (Player != null)
            {
                m_evacOffr = (E3DEvacOffrPlayer)Player;

                m_evacOffr.onLocEnterExitFunc.AddListener(Callback_AreaEnterExit);
                m_evacOffr.onLocVictimNumChangedFunc.AddListener(Callback_AreaVictimChanged);
                m_evacOffr.onResponseRecvFunc.AddListener(Callback_ResponseReceived);
                m_evacOffr.onAreaAmbulanceNumUpdateFunc += EvacPoint_AmbulanceNumUpdated;
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (m_AmbulanceDetailsGroup.activeInHierarchy && m_selAmbulance != null)
            {
                // update victim display
                if (m_selAmbulance.Victim != null)
                {
                    m_BtnLoadVictim.interactable = false;
                    m_BtnUnloadVictim.interactable = true;

                    AVictim victim = m_selAmbulance.Victim;

                    m_ImgVictimPortrait.sprite = victim.m_PortraitSprite;
                    m_ImgVictimSex.sprite = victim.m_Sex == ESex.Male ? m_MaleSexSprite : m_FemaleSexSprite;
                    m_TxtVictimName.text = victim.m_GivenName;
                    m_VictimHealthbar.value = victim.m_CurHealth / Consts.MAX_VICTIM_HEALTH;
                }
                else
                {
                    m_BtnLoadVictim.interactable = true;
                    m_BtnUnloadVictim.interactable = false;

                    m_ImgVictimPortrait.sprite = m_NullMaleSprite;
                    m_ImgVictimSex.sprite = m_MaleSexSprite;
                    m_TxtVictimName.text = "No victim loaded";
                    m_VictimHealthbar.value = 0;
                }

                // update hospital display
                if (m_selAmbulance.HasRoute && m_selAmbulance.Destination is AHospital)
                {
                    AHospital hospital = (AHospital)m_selAmbulance.Destination;

                    m_TxtHospitalName.text = hospital.m_PrintName;
                    m_TxtTimeToDest.text = m_selAmbulance.TravelTime.ToString() + " mins";
                    m_TxtDistToDest.text = m_selAmbulance.TravelDistance.ToString() + " km";
                }
                else
                {
                    m_TxtHospitalName.text = "Not selected";
                    m_TxtTimeToDest.text = "0 mins";
                    m_TxtDistToDest.text = "0 km";
                }
            }
        }

        private void Callback_AreaEnterExit(AVictimPlaceableArea oldArea, AVictimPlaceableArea newArea)
        {
            if (oldArea != null && oldArea == m_evacOffr.CurrentLocation)
            {
                ClearAmbulanceList();

                Player.CanMove = true;
            }

            if (newArea != null)
            {
                AEvacPoint evacPoint = (AEvacPoint)newArea;
                if (evacPoint != null)
                    PopulateAmbulanceList(evacPoint.GetAmbulances());

                // create the victim cards
                m_SelectVictimWindow.ClearVictimButtons();
                m_SelectVictimWindow.PopulateVictimButtons(newArea.GetVictims());

                m_AmbulanceListGroup.SetActive(true);

                Player.CanMove = false;
            }
        }

        private void Callback_AreaVictimChanged(EListOperation arg0, AVictim arg1, AVictim arg2)
        {
            if (m_evacOffr != null && m_evacOffr.CurrentLocation != null)
            {
                m_SelectVictimWindow.ClearVictimButtons();
                m_SelectVictimWindow.PopulateVictimButtons(m_evacOffr.CurrentLocation.GetVictims());
            }
        }

        private void EvacPoint_AmbulanceNumUpdated(EListOperation op, AAmbulance oldItem, AAmbulance newItem)
        {
            // update the ambulance list
            if (op == EListOperation.Add)
            {
                GUIAmbulanceListButton newBtn = Instantiate(m_AmbulanceListButtonPrefab);

                newBtn.gameObject.SetActive(true);
                newBtn.m_RectTransform.SetParent(m_AmbulanceListButtonContentTransform, false);
                newBtn.Ambulance = newItem;

                m_ambulanceButtons.Add(newBtn);

                if (m_selAmbulance == null)
                    SelectFirstButtonInAmbulanceList();
            }
            else if (op == EListOperation.Remove)
            {
                var list = m_ambulanceButtons.ToArray();
                int deleteIdx = -1;

                for (int i = 0; i < list.Length; i++)
                {
                    if (m_ambulanceButtons[i].Ambulance == oldItem)
                    {
                        Destroy(m_ambulanceButtons[i].gameObject);
                        deleteIdx = i;
                        break;
                    }
                }

                if (deleteIdx != -1)
                    m_ambulanceButtons.RemoveAt(deleteIdx);
            }
        }

        private void PopulateAmbulanceList(AAmbulance[] ambulances)
        {
            ClearAmbulanceList();

            for (int i = 0; i < ambulances.Length; i++)
            {
                GUIAmbulanceListButton newBtn = Instantiate(m_AmbulanceListButtonPrefab);
                    
                newBtn.gameObject.SetActive(true);
                newBtn.m_RectTransform.SetParent(m_AmbulanceListButtonContentTransform, false);
                newBtn.Ambulance = ambulances[i];

                m_ambulanceButtons.Add(newBtn);
            }

            SelectFirstButtonInAmbulanceList();
        }

        private void ClearAmbulanceList()
        {
            for (int i = 0; i < m_ambulanceButtons.Count; i++)
            {
                Destroy(m_ambulanceButtons[i].gameObject);
            }
            m_ambulanceButtons.Clear();
        }

        public void AmbulanceButton_Clicked(bool state, GUIAmbulanceListButton button)
        {
            if (state)
                m_selAmbulance = button.Ambulance;
            else
                m_selAmbulance = null;
        }

        public void UseAmbulance()
        {
            if (m_evacOffr == null)
                return;

            if (m_selAmbulance != null)
            {
                m_evacOffr.SetAmbulance(m_selAmbulance);

                if (m_selAmbulance.CurrentState == AAmbulance.EState.Moving)
                {
                    CanvasGroup ambulanceDetailsGroup = m_AmbulanceDetailsGroup.GetComponent<CanvasGroup>();
                    ambulanceDetailsGroup.interactable = false;
                    ambulanceDetailsGroup.blocksRaycasts = false;

                    m_BtnEvacuate.interactable = false;
                }
                else
                {
                    CanvasGroup ambulanceDetailsGroup = m_AmbulanceDetailsGroup.GetComponent<CanvasGroup>();
                    ambulanceDetailsGroup.interactable = true;
                    ambulanceDetailsGroup.blocksRaycasts = true;

                    m_BtnEvacuate.interactable = true;
                }
            }
            else
            {
                CanvasGroup ambulanceDetailsGroup = m_AmbulanceDetailsGroup.GetComponent<CanvasGroup>();
                ambulanceDetailsGroup.interactable = false;
                ambulanceDetailsGroup.blocksRaycasts = false;

                m_BtnEvacuate.interactable = false;
            }

            m_AmbulanceListBlocker.SetActive(true);
            m_AmbulanceDetailsGroup.SetActive(true);
            m_ActionButtonsGroup.SetActive(true);
        }

        public void CancelAmbulanceSelect()
        {
            if (m_evacOffr == null)
                return;

            if (m_selAmbulance != null && m_selAmbulance.CurrentState == AAmbulance.EState.Idle)
            {
                m_evacOffr.CurrentAmbulance?.CMD_ClearRoute();

                m_evacOffr.UnloadVictimFrmAmbulance();
                m_evacOffr.SetAmbulance(null);
            }

            //m_selAmbulance = null;

            CanvasGroup ambulanceDetailsGroup = m_AmbulanceDetailsGroup.GetComponent<CanvasGroup>();
            ambulanceDetailsGroup.interactable = true;
            ambulanceDetailsGroup.blocksRaycasts = true;

            m_BtnEvacuate.interactable = true;

            m_AmbulanceListBlocker.SetActive(false);
            m_AmbulanceDetailsGroup.SetActive(false);
            m_ActionButtonsGroup.SetActive(false);

            //SelectFirstButtonInAmbulanceList();
        }

        // Victim
        #region Vicitm

        public void OpenLoadAmbulanceWindow()
        {
            if (m_evacOffr == null)
                return;

            m_SelectVictimWindow.gameObject.SetActive(true);

            m_SelectVictimWindow.ClearVictimButtons();
            m_SelectVictimWindow.PopulateVictimButtons(m_evacOffr.CurrentLocation.GetVictims());
        }

        private void M_SelectVictimWindow_onOkBtnClickedFunc(AVictim victim)
        {
            if (m_evacOffr == null)
                return;

            m_evacOffr.LoadVictimToAmbulance(victim);
        }

        private void M_SelectVictimWindow_onCancelBtnClickedFunc()
        {
        }

        public void UnloadVictim()
        {
            if (m_evacOffr == null)
                return;

            m_evacOffr.UnloadVictimFrmAmbulance();
        }

        #endregion

        // Hospital
        #region Hospital

        public void OpenSelectHospitalWindow()
        {
            m_HospitalSelectWindow.gameObject.SetActive(true);
        }

        private void M_HospitalSelectWindow_onOkBtnClickedFunc(Route hospitalRoute)
        {
            if (m_evacOffr == null)
                return;

            m_evacOffr.SetHospitalToAmbulance(hospitalRoute);
        }

        private void M_HospitalSelectWindow_onCancelBtnClickedFunc()
        {
        }

        #endregion

        public void EvacuateAmbulance()
        {
            if (m_evacOffr == null)
                return;

            m_evacOffr.EvacuateVictim();

            // NOTE: response for this function is below under "success"
        }

        private void SelectFirstButtonInAmbulanceList()
        {
            if (m_ambulanceButtons.Count > 0)
            {
                m_ambulanceButtons[0].m_Toggle.isOn = true;
                m_selAmbulance = m_ambulanceButtons[0].Ambulance;
            }
        }

        private void Callback_ResponseReceived(ActorNetResponse_s response)
        {
            if (response.m_ResponseType.Equals("error") || response.m_ResponseType.Equals("generic"))
            {
                ShowTextBoxPrompt(response.m_Message);
            }
            else if (response.m_ResponseType.Equals("ambulance_load_changed"))
            {
                ShowTextBoxPrompt(response.m_Message);
            }
            else if (response.m_ResponseType.Equals("cannot_load_victim"))
            {
                ShowTextBoxPrompt(response.m_Message);
            }
            else if (response.m_ResponseType.Equals("success"))
            {
                ShowTextBoxPrompt(response.m_Message, () => 
                {
                    CanvasGroup ambulanceDetailsGroup = m_AmbulanceDetailsGroup.GetComponent<CanvasGroup>();
                    ambulanceDetailsGroup.interactable = true;
                    ambulanceDetailsGroup.blocksRaycasts = true;

                    m_BtnEvacuate.interactable = true;

                    m_AmbulanceListBlocker.SetActive(false);
                    m_AmbulanceDetailsGroup.SetActive(false);
                    m_ActionButtonsGroup.SetActive(false);

                    //SelectFirstButtonInAmbulanceList();
                });
            }
        }
    }
}
