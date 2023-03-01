using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public abstract class GUIPopupboxBase : GUIPopupBase
{
    public const int MAX_CHAR = 260;

    public TMPro.TMP_InputField m_TxtMsg;

    protected UnityAction<int> responseFunc;


    protected void SetMessage(string message)
    {
        StringBuilder sb = new StringBuilder(message, MAX_CHAR);
        m_TxtMsg.text = sb.ToString();
    }
}
