using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace E3D
{
    [DisallowMultipleComponent]
    public class GUIPopupRenameLocation : GUIPopupBase
    {
        public Button m_BtnOk;
        public Button m_BtnCancel;
        public TMP_InputField m_InputField;

        private string m_oldName;
        private ALocationPoint m_location;

        protected UnityAction<ALocationPoint, int> responseFunc;


        protected override void Awake()
        {
            base.Awake();
            m_BtnOk.onClick.AddListener(OkButtonPressedCallback);
            m_BtnCancel.onClick.AddListener(CancelButtonPressedCallback);
        }

        public void Open(ALocationPoint location, string okBtnString = null, string cancelBtnString = null, UnityAction<ALocationPoint, int> response = null)
        {
            m_location = location;

            m_oldName = location.m_PrintName;
            m_InputField.text = m_oldName;

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
            if (string.IsNullOrEmpty(m_InputField.text))
            {
                m_location.CMD_SetPrintName(m_oldName);
            }
            else
            {
                m_location.CMD_SetPrintName(m_InputField.text);
            }

            if (responseFunc != null)
                responseFunc.Invoke(m_location, 0);

            SetVisible(false);
        }

        private void CancelButtonPressedCallback()
        {
            m_location.CMD_SetPrintName(m_oldName);

            if (responseFunc != null)
                responseFunc.Invoke(m_location, 1);

            SetVisible(false);
        }
    }
}
