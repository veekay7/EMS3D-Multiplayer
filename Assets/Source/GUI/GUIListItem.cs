using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class GUIListItem : GUIBase
{
    [HideInInspector]
    public Toggle m_Toggle;
    public TMP_Text m_Label;

    public GameObject Owner { get; set; }

    public string ClassName { get; set; }

    public string LabelString
    {
        get { return m_Label.text; }
        set { m_Label.text = value; }
    }

    protected override void Awake()
    {
        base.Awake();
        m_Toggle = GetComponent<Toggle>();
    }

    public void OnToggleStateChanged(bool state)
    {
        if (Owner == null)
            return;

        ExecuteEvents.Execute<IStateChangedHandler>(Owner, null,
                        (handler, e) => { handler.ListItem_StateChanged(this, state); });
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        m_Toggle = GetComponent<Toggle>();
    }


    public interface IStateChangedHandler : IEventSystemHandler
    {
        void ListItem_StateChanged(GUIListItem item, bool state);
    }
}
