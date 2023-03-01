using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace E3D
{
    public class GUIPopupAddBot : GUIPopupBase
    {
        public Button m_BtnOk;
        public Button m_BtnCancel;
        public TMP_Dropdown m_RoleCombobox;
        public TMP_InputField m_PlayerNameTxtbox;
        public TMP_Text m_TitleTxt;
        public TMP_Text m_CostTxt;

        private EEmtRole m_role;

        public UnityAction<int, EEmtRole, string> responseFunc;


        protected override void Awake()
        {
            base.Awake();
            m_BtnOk.onClick.AddListener(OkButtonPressedCallback);
            m_BtnCancel.onClick.AddListener(CancelButtonPressedCallback);
        }

        public void Open(EEmtRole role, string okBtnString = null, string cancelBtnString = null, UnityAction<int, EEmtRole, string> response = null)
        {
            m_role = role;

            switch(role)
            {
                case EEmtRole.TriageOffr:
                    var triageOffr = GameCtrl.Instance.FindPrefab("AE3DCpu_TriageOffr").GetComponent<E3DPlayer>();
                    m_CostTxt.text = triageOffr.m_Cost.ToString();
                    m_TitleTxt.text = "Add Triage Officer";
                    break;

                case EEmtRole.FirstAidPointDoc:
                    var firstAidDoc = GameCtrl.Instance.FindPrefab("AE3DCpu_FirstAidDoc").GetComponent<E3DPlayer>();
                    m_CostTxt.text = firstAidDoc.m_Cost.ToString();
                    m_TitleTxt.text = "Add First Aid Point Doctor";
                    break;

                case EEmtRole.EvacOffr:
                    var evacOffr = GameCtrl.Instance.FindPrefab("AE3DCpu_EvacOffr").GetComponent<E3DPlayer>();
                    m_CostTxt.text = evacOffr.m_Cost.ToString();
                    m_TitleTxt.text = "Add Evacuation Officer";
                    break;
            }
            

            if (!string.IsNullOrEmpty(okBtnString))
                m_BtnOk.GetComponent<RectTransform>().GetChild(0).GetComponent<TMP_Text>().text = okBtnString;

            if (!string.IsNullOrEmpty(cancelBtnString))
                m_BtnCancel.GetComponent<RectTransform>().GetChild(0).GetComponent<TMP_Text>().text = cancelBtnString;

            responseFunc = response;
        }

        protected override void HandleKbInput()
        {
            if (Input.GetKeyDown(KeyCode.Return))
                OkButtonPressedCallback();
            else if (Input.GetKeyDown(KeyCode.Escape))
                CancelButtonPressedCallback();
        }

        private void OkButtonPressedCallback()
        {
            if (string.IsNullOrEmpty(m_PlayerNameTxtbox.text))
            {
                ((TMP_Text)m_PlayerNameTxtbox.placeholder).text = "Name cannot be empty.";
            }
            else
            {
                if (responseFunc != null)
                    responseFunc.Invoke(0, m_role, m_PlayerNameTxtbox.text);

                SetVisible(false);
            }
        }

        private void CancelButtonPressedCallback()
        {
            if (responseFunc != null)
                responseFunc.Invoke(1, EEmtRole.Spectator, null);

            SetVisible(false);
        }
    }
}
