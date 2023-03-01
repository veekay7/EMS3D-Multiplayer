using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace E3D
{
    public class GUIClassSelectScreen : GUIScreen
    {
        [Header("Class Select Screen")]
        public Button m_BtnConfirm;
        public TMP_Text m_TxtSelected;

        private int m_emtId;


        protected override void Awake()
        {
            base.Awake();

            m_Classname = "ClassSelect";
            m_emtId = -1;
        }

        protected override void Reset()
        {
            base.Reset();
            m_Classname = "ClassSelect";
        }

        private void LateUpdate()
        {
            m_BtnConfirm.interactable = m_emtId >= 0;
        }

        public void SelectPlayerClass(int classId)
        {
            if (GameCtrl.Instance == null)
            {
                Debug.Log("No GameSystem in the scene, cannot spawn EMT actor.");
                return;
            }

            // TODO: fix this shit so that the best you can pick is evac offr
            m_emtId = classId;
            switch (m_emtId)
            {
                case Consts.EMT_TRIAGE_OFFR:
                    m_TxtSelected.text = ("triage officer").ToUpper();
                    break;

                case Consts.EMT_FIRST_AID_DOC:
                    m_TxtSelected.text = ("first aid point doctor").ToUpper();
                    break;

                case Consts.EMT_EVAC_OFFR:
                    m_TxtSelected.text = ("evacuation officer").ToUpper();
                    break;

                default:
                    m_TxtSelected.text = "-";
                    break;
            }
        }

        public void PossessSelectedActor()
        {
            if (m_emtId < 1 || m_emtId > 3)
                return;

            if (E3DPlayer.Local != null)
            {
                //E3DPlayer.Local.CMD_RequestSpawnEmt(m_emtId);
            }
        }
    }
}
