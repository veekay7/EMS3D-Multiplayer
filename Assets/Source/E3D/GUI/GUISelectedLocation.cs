using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace E3D
{
    public class GUISelectedLocation : GUIBase
    {
        public TMP_Text m_LocationNameTxt;
        public Image m_ThumbnailImg;
        public CasualtyPointContent_s m_CasualtyPointContent = default;
        public FAPContent_s m_FAPContent = default;
        public EvacContent_s m_EvacContent = default;

        private ALocationPoint m_location;


        private void LateUpdate()
        {
            if (m_location != null)
            {
                m_LocationNameTxt.text = m_location.m_PrintName;

                if (m_location is AFirstAidPoint)
                {
                    AFirstAidPoint fap = (AFirstAidPoint)m_location;
                    m_FAPContent.m_NumPlayersTxt.text = fap.NumPlayers.ToString();
                    m_FAPContent.m_NumVictimsTxt.text = fap.NumVictims.ToString();
                }
                else if (m_location is AEvacPoint)
                {
                    AEvacPoint evacPoint = (AEvacPoint)m_location;
                    m_EvacContent.m_NumPlayersTxt.text = evacPoint.NumPlayers.ToString();
                    m_EvacContent.m_NumVictimsTxt.text = evacPoint.NumVictims.ToString();
                    m_EvacContent.m_NumAmbulancesTxt.text = evacPoint.NumAmbulances.ToString();
                }
                else if (m_location is AVictimPlaceableArea)
                {
                    AVictimPlaceableArea casualtyPoint = (AVictimPlaceableArea)m_location;
                    m_CasualtyPointContent.m_NumPlayersTxt.text = casualtyPoint.NumPlayers.ToString();
                    m_CasualtyPointContent.m_NumVictimsTxt.text = casualtyPoint.NumVictims.ToString();
                }
            }
        }

        public void Open(ALocationPoint location)
        {
            Clear();

            m_location = location;

            if (m_location != null)
            {
                m_ThumbnailImg.sprite = m_location.m_ThumbnailSprite;

                if (m_location is AFirstAidPoint)
                {
                    m_FAPContent.m_Content.SetActive(true);
                }
                else if (m_location is AEvacPoint)
                {
                    m_EvacContent.m_Content.SetActive(true);
                }
                else if (m_location is AVictimPlaceableArea)
                {
                    m_CasualtyPointContent.m_Content.SetActive(true);
                }
            }

            gameObject.SetActive(true);
        }

        public void Close()
        {
            m_location = null;
            Clear();

            gameObject.SetActive(false);
        }

        private void Clear()
        {
            m_LocationNameTxt.text = "Location Name";
            m_ThumbnailImg.sprite = null;

            m_CasualtyPointContent.m_NumPlayersTxt.text = "0";
            m_CasualtyPointContent.m_NumVictimsTxt.text = "0";
            m_CasualtyPointContent.m_TriageRateTxt.text = "0";

            m_FAPContent.m_NumPlayersTxt.text = "0";
            m_FAPContent.m_NumVictimsTxt.text = "0";
            m_FAPContent.m_TreatmentRateTxt.text = "0";

            m_EvacContent.m_NumPlayersTxt.text = "0";
            m_EvacContent.m_NumVictimsTxt.text = "0";
            m_EvacContent.m_EvacRateTxt.text = "0";
            m_EvacContent.m_NumAmbulancesTxt.text = "0";

            m_CasualtyPointContent.m_Content.SetActive(false);
            m_FAPContent.m_Content.SetActive(false);
            m_EvacContent.m_Content.SetActive(false);
        }
    }

    [Serializable]
    public struct CasualtyPointContent_s
    {
        public GameObject m_Content;
        public TMP_Text m_NumPlayersTxt;
        public TMP_Text m_NumVictimsTxt;
        public TMP_Text m_TriageRateTxt;
    }

    [Serializable]
    public struct FAPContent_s
    {
        public GameObject m_Content;
        public TMP_Text m_NumPlayersTxt;
        public TMP_Text m_NumVictimsTxt;
        public TMP_Text m_TreatmentRateTxt;
    }

    [Serializable]
    public struct EvacContent_s
    {
        public GameObject m_Content;
        public TMP_Text m_NumPlayersTxt;
        public TMP_Text m_NumVictimsTxt;
        public TMP_Text m_NumAmbulancesTxt;
        public TMP_Text m_EvacRateTxt;
    }
}
