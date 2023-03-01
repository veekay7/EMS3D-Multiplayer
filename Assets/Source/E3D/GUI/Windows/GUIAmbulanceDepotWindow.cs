using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace E3D
{
    public class GUIAmbulanceDepotWindow : GUIBase
    {
        public enum WindowMode { CheckMode, DeploymentMode }

        public Button m_ButtonPrefab;
        public RectTransform m_BtnsContentTransform;

        public TMP_Text m_TxtName;
        public TMP_Text m_TxtDist;
        public TMP_Text m_TxtTravelTime;
        public TMP_Text m_TxtNumAmbulance;
        public TMP_Text m_TxtAmbulanceCost;
        public GameObject m_AmbulanceCostInfoSection;

        public Button m_BtnOk;

        private List<Button> m_buttons = new List<Button>();

        public event UnityAction<AAmbulanceDepot> onOkBtnClickedFunc;

        public WindowMode Mode { get; set; }

        public AAmbulanceDepot Depot { get; private set; }

        //public AEvacPoint TargetEvacPoint { get; private set; }


        private void Start()
        {
            // set the display for cost of ambulance
            // TODO: this is quite inefficient, but it will do for now...
            GameObject ambulancePrefab = GameCtrl.Instance.FindPrefab("AAmbulance");
            if (ambulancePrefab != null)
                m_TxtAmbulanceCost.text = ambulancePrefab.GetComponent<AAmbulance>().Cost.ToString();
            else
                m_TxtAmbulanceCost.text = "0";
        }

        private void OnEnable()
        {
            if (Mode == WindowMode.CheckMode)
            {
                m_AmbulanceCostInfoSection.SetActive(false);
                m_BtnOk.gameObject.SetActive(false);
            }
            else
            {
                m_AmbulanceCostInfoSection.SetActive(true);
                m_BtnOk.gameObject.SetActive(true);
            }

            m_BtnOk.interactable = false;

            if (GameState.Current != null)
            {
                var ambulanceDepots = GameState.Current.m_AmbulanceDepots.ToArray();
                for (int i = 0; i < ambulanceDepots.Length; i++)
                {
                    var newBtn = Instantiate(m_ButtonPrefab);

                    newBtn.gameObject.SetActive(true);
                    newBtn.transform.GetChild(0).GetComponent<TMP_Text>().text = ambulanceDepots[i].m_PrintName;
                    newBtn.transform.SetParent(m_BtnsContentTransform, false);

                    m_buttons.Add(newBtn);
                }

                if (m_buttons.Count > 0)
                    m_buttons[0].onClick.Invoke();
            }
        }

        private void OnDisable()
        {
            Clear();
        }

        public void Callback_OkBtnClicked()
        {
            if (onOkBtnClickedFunc != null)
                onOkBtnClickedFunc.Invoke(Depot);

            Clear();
            gameObject.SetActive(false);
        }

        public void Callback_ListButtonClicked(GameObject button)
        {
            m_TxtName.text = "Not selected";
            m_TxtDist.text = "0 km";
            m_TxtTravelTime.text = "0 minutes";
            m_TxtNumAmbulance.text = "0";

            Depot = null;
            m_BtnOk.interactable = false;

            // find the index of the button
            var buttonComponent = button.GetComponent<Button>();
            int buttonIndex = m_buttons.IndexOf(buttonComponent);

            if (buttonIndex != -1)
            {
                // get the selected depot
                var ambulanceDepots = GameState.Current.m_AmbulanceDepots.ToArray();
                Depot = ambulanceDepots[buttonIndex];

                // get the route and show that shit
                var route = GameState.Current.m_RouteController.GetRoute(Depot.m_LocationId);

                m_TxtName.text = Depot.m_PrintName;
                m_TxtNumAmbulance.text = Depot.NumAmbulancesLeft.ToString();

                m_TxtDist.text = route.m_Distance.ToString() + " km";
                m_TxtTravelTime.text = route.m_TravelTime + " minutes";

                m_BtnOk.interactable = true;
            }
        }

        private void Clear()
        {
            Depot = null;
            //TargetEvacPoint = null;

            for (int i = 0; i < m_buttons.Count; i++)
            {
                Destroy(m_buttons[i].gameObject);
            }
            m_buttons.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
