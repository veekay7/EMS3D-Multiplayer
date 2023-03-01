using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace E3D
{
    public class GUITrainingScreen : GUIScreen
    {
        [Header("Training Screen")]
        public TMP_Dropdown m_MapDrawer;
        public Slider m_NumVictimsSlider;
        public Slider m_P3VictimsSlider;
        public Slider m_P2VictimsSlider;
        public Slider m_P1VictimsSlider;
        public Slider m_P0VictimsSlider;

        private int m_selectedMapIdx;
        private bool m_mapListPopulated;


        protected override void Awake()
        {
            base.Awake();
            m_selectedMapIdx = -1;
        }

        public override void Open(UnityAction onFinishAnim = null)
        {
            if (!m_mapListPopulated)
            {
                PopulateMapDrawer();

                m_mapListPopulated = true;
            }

            if (m_MapDrawer.options.Count > 0)
                m_MapDrawer.onValueChanged.Invoke(0);

            m_NumVictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_NumVictims;
            m_P3VictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_Probability_P3;
            m_P2VictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_Probability_P2;
            m_P1VictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_Probability_P1;
            m_P0VictimsSlider.value = GameCtrl.Instance.m_VictimSettings.m_Probability_P0;

            base.Open(onFinishAnim);
        }

        public void SelectMap(int index)
        {
            m_selectedMapIdx = index;
        }

        public void StartGame()
        {
            // set up with game system
            if (GameCtrl.Instance == null)
            {
                Debug.Log("Cannot start the game, GameCtrl.Instance is null");
            }
            else
            {
                if (m_selectedMapIdx != -1)
                {
                    MapListEntry map = GameCtrl.MapList[m_selectedMapIdx];

                    if (string.IsNullOrEmpty(map.m_SceneFilename))
                    {
                        Debug.Log("Cannot load scene, scene file name is empty in " + map.name);
                        return;
                    }

                    SetInteractable(false);

                    Globals.m_CurrentMap = map;
                    GameCtrl.Instance.m_VictimSettings.m_NumVictims = (int)m_NumVictimsSlider.value;
                    GameCtrl.Instance.m_VictimSettings.m_Probability_P3 = (int)m_P3VictimsSlider.value;
                    GameCtrl.Instance.m_VictimSettings.m_Probability_P2 = (int)m_P2VictimsSlider.value;
                    GameCtrl.Instance.m_VictimSettings.m_Probability_P1 = (int)m_P1VictimsSlider.value;
                    GameCtrl.Instance.m_VictimSettings.m_Probability_P0 = (int)m_P0VictimsSlider.value;
                    
                    ScreenWiper.Instance.DoFade(ScreenWiper.FillMode.Fill, 1.0f, 0.0f, () =>
                    {
                        if (GUIController.Instance.ActiveScreen == this)
                            GUIController.Instance.CloseCurrentScreen();

                        SceneLoader.Instance.LoadScene(map.m_SceneFilename);
                    });
                }
            }
        }

        private void PopulateMapDrawer()
        {
            m_MapDrawer.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            var mapList = GameCtrl.MapList;

            for (int i = 0; i < mapList.Length; i++)
            {
                string label = mapList[i].m_DisplayName;
                options.Add(new TMP_Dropdown.OptionData(label));
            }

            m_MapDrawer.AddOptions(options);
            if (m_MapDrawer.options.Count > 0)
            {
                m_MapDrawer.value = 0;
            }
        }
    }
}
