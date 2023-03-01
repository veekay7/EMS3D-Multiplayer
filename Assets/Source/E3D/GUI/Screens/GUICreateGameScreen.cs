using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace E3D
{
    public class GUICreateGameScreen : GUIScreen
    {
        [Header("Create Game Screen")]
        // server settings
        public TMP_Dropdown m_MapListDropdown;
        public TMP_Dropdown m_DifficultyDropdown;
        public TMP_InputField m_HostnameTxtbox;
        public TMP_InputField m_ServerPwTxtbox;
        public Slider m_MaxPlayersSlider;
        public Toggle m_EnableBotsToggle;
        public Slider m_NumVictimsSlider;
        public Slider m_P3VictimsSlider;
        public Slider m_P2VictimsSlider;
        public Slider m_P1VictimsSlider;
        public Slider m_P0VictimsSlider;

        // server defailts
        public Image m_MapImg;
        public TMP_Text m_MapNameTxt;
        public TMP_Text m_DifficultyTxt;
        public TMP_Text m_NumVictimsTxt;
        public TMP_Text m_MaxPlayersTxt;

        private List<string> m_mapListOptions = new List<string>();
        private List<string> m_difficultyOptions = new List<string>();


        protected override void Awake()
        {
            base.Awake();
        }

        public override void Open(UnityAction onFinishAnim = null)
        {
            PopulateMapListGUI();

            // populate difficulty enums 
            m_difficultyOptions.Clear();

            string[] difficultyEnumNames = Enum.GetNames(typeof(EDifficulty));
            for (int i = 0; i < difficultyEnumNames.Length; i++)
            {
                m_difficultyOptions.Add(difficultyEnumNames[i]);
            }
            m_DifficultyDropdown.AddOptions(m_difficultyOptions);
            m_DifficultyDropdown.onValueChanged.Invoke(m_DifficultyDropdown.value);

            // reset text boxes, slides and other values
            m_HostnameTxtbox.text = GameCtrl.Instance.m_Hostname;
            m_NumVictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_NumVictims;
            m_P3VictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_Probability_P3;
            m_P2VictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_Probability_P2;
            m_P1VictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_Probability_P1;
            m_P0VictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_Probability_P0;

            base.Open(onFinishAnim);
        }

        private void Update()
        {
            m_NumVictimsTxt.text = m_NumVictimsSlider.value.ToString();
            m_MaxPlayersTxt.text = m_MaxPlayersSlider.value.ToString();
        }

        public void HostGame()
        {
            ResolveEmptyHostname();

            if (GUIController.Instance.ActiveScreen == this)
                GUIController.Instance.CloseCurrentScreen();

            Globals.m_ConnectGameType = 1;

            // NOTE: current map can be null
            GameCtrl.Instance.GameplayScene = Globals.m_CurrentMap?.m_SceneFilename;

            GameCtrl.Instance.maxConnections = (int)m_MaxPlayersSlider.value;
            GameCtrl.Instance.m_EnableBots = m_EnableBotsToggle.isOn;
            GameCtrl.Instance.m_VictimSettings.m_NumVictims = (int)m_NumVictimsSlider.value;
            GameCtrl.Instance.m_VictimSettings.m_Probability_P3 = (int)m_P3VictimsSlider.value;
            GameCtrl.Instance.m_VictimSettings.m_Probability_P2 = (int)m_P2VictimsSlider.value;
            GameCtrl.Instance.m_VictimSettings.m_Probability_P1 = (int)m_P1VictimsSlider.value;
            GameCtrl.Instance.m_VictimSettings.m_Probability_P0 = (int)m_P0VictimsSlider.value;
            
            GameCtrl.Instance.CreateLobby();
        }

        private void PopulateMapListGUI()
        {
            m_mapListOptions.Clear();
            
            var mapList = GameCtrl.MapList;
            for (int i = 0; i < mapList.Length; i++)
            {
                var cur = mapList[i];
                m_mapListOptions.Add(cur.m_DisplayName);
            }

            m_MapListDropdown.AddOptions(m_mapListOptions);
            m_MapListDropdown.onValueChanged.Invoke(m_MapListDropdown.value);
        }

        public void HostnameInputField_EndEdit(string hostnameString)
        {
            // set to globals
            GameCtrl.Instance.m_Hostname = hostnameString;
            ResolveEmptyHostname();
        }

        private void ResolveEmptyHostname()
        {
            // validate hostname string not empty
            if (string.IsNullOrEmpty(GameCtrl.Instance.m_Hostname))
            {
                GameCtrl.Instance.m_Hostname = "New MP Game";
                m_HostnameTxtbox.text = GameCtrl.Instance.m_Hostname;
            }
        }

        public void PwInputField_EndEdit(string pwString)
        {
            // TODO: set to authenticator
        }

        public void MapListDrawer_ValueChanged(int index)
        {
            var mapList = GameCtrl.MapList;
            Globals.m_CurrentMap = mapList[index];

            m_MapImg.sprite = mapList[index].m_Thumbnail;
            m_MapNameTxt.text = mapList[index].m_DisplayName;
        }

        public void DifficultyDrawer_ValueChanged(int index)
        {
            GameCtrl.Instance.m_Difficulty = (EDifficulty)index;
            m_DifficultyTxt.text = m_difficultyOptions[index];
        }
    }
}
