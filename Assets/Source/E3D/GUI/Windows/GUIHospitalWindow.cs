using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace E3D
{
    public class GUIHospitalWindow : GUIBase
    {
        public enum WindowMode { CheckMode, Usable }

        public Sprite m_NullLogoSprite;
        public Image m_ImgLogo;
        public TMP_Text m_TxtCapacity;
        public TMP_Text m_TxtDist;
        public TMP_Text m_TxtTravelTime;
        public TMP_Text m_TxtSpecialties;

        public Button m_ButtonPrefab;
        public RectTransform m_BtnsContentTransform;

        public Button m_BtnOk;
        public Button m_BtnCancel;

        private GameState m_gameState;
        private List<Button> m_buttons = new List<Button>();

        public event UnityAction<Route> onOkBtnClickedFunc;
        public event UnityAction onCancelBtnClickedFunc;

        public WindowMode Mode { get; set; }

        public AHospital Hospital { get; private set; }

        public Route HospitalRoute { get; private set; }


        protected override void Awake()
        {
            base.Awake();
            m_gameState = null;
        }

        private void OnEnable()
        {
            if (Mode == WindowMode.CheckMode)
                m_BtnOk.gameObject.SetActive(false);
            else
                m_BtnOk.gameObject.SetActive(true);

            m_BtnOk.interactable = false;

            m_gameState = GameState.Current;
            if (m_gameState != null)
            {
                var hospitals = m_gameState.m_Hospitals.ToArray();
                for (int i = 0; i < hospitals.Length; i++)
                {
                    var newBtn = Instantiate(m_ButtonPrefab);

                    newBtn.gameObject.SetActive(true);
                    newBtn.transform.GetChild(0).GetComponent<TMP_Text>().text = hospitals[i].m_PrintName;
                    newBtn.transform.SetParent(m_BtnsContentTransform, false);

                    m_buttons.Add(newBtn);
                }

                // if there are buttons, select the first one as the option
                if (m_buttons.Count > 0)
                {
                    m_buttons[0].onClick.Invoke();
                }
            }
        }

        // called by GUI buttons set in editor
        public void Callback_ButtonClicked(GameObject button)
        {
            ClearSelectedInfo();

            Hospital = null;
            m_BtnOk.interactable = false;

            // find the index of the button and if there is a match, we update the display
            var buttonComponent = button.GetComponent<Button>();
            int buttonIndex = m_buttons.IndexOf(buttonComponent);

            if (buttonIndex != -1)
            {
                // get the selected hospital
                var hospitals = m_gameState.m_Hospitals.ToArray();
                Hospital = hospitals[buttonIndex];

                // get the route and show that shit
                var route = GameState.Current.m_RouteController.GetRoute(Hospital.m_LocationId);
                HospitalRoute = route;

                m_ImgLogo.sprite = Hospital.m_Logo != null ? Hospital.m_Logo : null;
                m_TxtSpecialties.text = Hospital.m_Specialties.ToString();

                RefreshInfo();

                m_BtnOk.interactable = true;
            }
        }

        public void Callback_OkBtnClicked()
        {
            if (onOkBtnClickedFunc != null)
                onOkBtnClickedFunc.Invoke(HospitalRoute);

            Clear();
            gameObject.SetActive(false);
        }

        public void Callback_CancelBtnClicked()
        {
            if (onCancelBtnClickedFunc != null)
                onCancelBtnClickedFunc.Invoke();

            Clear();
            gameObject.SetActive(false);
        }

        private void RefreshInfo()
        {
            if (HospitalRoute == null || Hospital == null)
                return;

            string capacityString = Hospital.NumVictims.ToString() + " / " + Hospital.m_Capacity.ToString();
            string distString = HospitalRoute.m_Distance.ToString();
            string timeString = HospitalRoute.m_TravelTime.ToString();

            m_TxtCapacity.text = capacityString;
            m_TxtTravelTime.text = distString;
            m_TxtDist.text = timeString;
        }

        private void ClearSelectedInfo()
        {
            m_ImgLogo.sprite = m_NullLogoSprite;
            m_TxtCapacity.text = "Unknown";
            m_TxtTravelTime.text = "Unknown";
            m_TxtDist.text = "Unknown";
            m_TxtSpecialties.text = "No specialties available.";
        }

        private void Clear()
        {
            Hospital = null;
            HospitalRoute = null;

            for (int i = 0; i < m_buttons.Count; i++)
            {
                Destroy(m_buttons[i].gameObject);
            }
            m_buttons.Clear();
            ClearSelectedInfo();
        }

        private void OnDisable()
        {
            Clear();
        }

        protected void OnDestroy()
        {
            Clear();
        }
    }
}
