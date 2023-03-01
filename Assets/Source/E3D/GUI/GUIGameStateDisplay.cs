using UnityEngine;
using TMPro;
using System;

namespace E3D
{
    public class GUIGameStateDisplay : MonoBehaviour
    {
        //public TMP_Text m_TxtIncidentScene;
        public TMP_Text m_TxtCurrLocation;
        public TMP_Text m_TxtTime;
        public TMP_Text m_TxtVictimCount;

        public E3DPlayer Player { get; set; }


        private void LateUpdate()
        {
            if (GameMode.Current != null && GameState.Current != null)
            {
                // update game time
                var timeSpan = TimeSpan.FromSeconds(GameMode.Current.ElapsedGameTime);
                m_TxtTime.text = string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);

                //// update incident scene text
                //if (Globals.m_CurrentMap != null)
                //{
                //    m_TxtIncidentScene.text = Globals.m_CurrentMap.m_DisplayName;
                //}

                // update victims left
                int victimsLeft = GameState.Current.m_Victims.Count;

                if (Player != null && Player != null && Player is E3DEmtPlayerBase)
                {
                    E3DEmtPlayerBase basePlayer = (E3DEmtPlayerBase)Player;

                    if (basePlayer is E3DTriageOffrPlayer)
                        victimsLeft -= GameState.Current.m_NumTriaged;

                    if (basePlayer is E3DFirstAidDocPlayer)
                        victimsLeft -= GameState.Current.m_NumTreated;

                    if (basePlayer is E3DEvacOffrPlayer)
                        victimsLeft -= GameState.Current.m_NumEvacuated;

                    if (basePlayer.CurrentLocation != null)
                        m_TxtCurrLocation.text = basePlayer.CurrentLocation.m_PrintName;
                    else
                        m_TxtCurrLocation.text = "Outside";
                }

                m_TxtVictimCount.text = victimsLeft.ToString();
            }
        }
    }
}
