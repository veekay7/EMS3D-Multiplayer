using TMPro;

namespace E3D
{
    public class GUIPlayerListItem : GUIBase
    {
        public TMP_Text m_NameTxt;
        public TMP_Text m_RoleTxt;


        public void SetPlayer(E3DPlayer player)
        {
            m_NameTxt.text = player.m_PlayerName;

            if (player is E3DTriageOffrPlayer || player is E3DTriageOffrCpu)
            {
                m_RoleTxt.text = Globals.m_EmtRoleStrings[EEmtRole.TriageOffr];
            }
            else if (player is E3DFirstAidDocPlayer || player is E3DFirstAidDocCpu)
            {
                m_RoleTxt.text = Globals.m_EmtRoleStrings[EEmtRole.FirstAidPointDoc];
            }
            else if (player is E3DEvacOffrPlayer || player is E3DEvacOffrCpu)
            {
                m_RoleTxt.text = Globals.m_EmtRoleStrings[EEmtRole.EvacOffr];
            }
        }
    }
}
