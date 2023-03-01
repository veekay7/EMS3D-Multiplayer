using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUILobbyPlayerListItem : GUIBase
{
    public TMP_Text m_PlayerIdTxt;
    public TMP_Text m_PlayerNameTxt;
    public TMP_Text m_RoleTxt;
    public Image m_ReadyImg;

    public Sprite m_CheckmarkSprite;
    public Sprite m_CrossSprite;

    public Color m_ReadyColor;
    public Color m_NotReadyColor;


    public E3DLobbyPlayer LobbyPlayer;


    private void LateUpdate()
    {
        if (LobbyPlayer != null)
        {
            m_PlayerIdTxt.text = LobbyPlayer.index.ToString();
            m_PlayerNameTxt.text = LobbyPlayer.m_PlayerName;
            m_RoleTxt.text = Globals.m_EmtRoleStrings[LobbyPlayer.m_SelectedRole];
            
            m_ReadyImg.sprite = LobbyPlayer.readyToBegin ? m_CheckmarkSprite : m_CrossSprite;
            m_ReadyImg.color = LobbyPlayer.readyToBegin ? m_ReadyColor : m_NotReadyColor;
        }
    }
}
