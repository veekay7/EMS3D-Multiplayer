using Mirror;
using TMPro;

namespace E3D
{
    public class GUIConnPlayerItem : GUIBase
    {
        public TMP_Text m_PlayerNameTxt;
        public TMP_Text m_RoleTxt;
        public TMP_Text m_LocationTxt;

        public NetworkConnection NetConnection { get; private set; }

        public E3DPlayer Player { get; private set; }


        private void LateUpdate()
        {
            if (NetConnection != null && Player != null)
            {
                m_PlayerNameTxt.text = Player.m_PlayerName;
                //m_RoleTxt.text = Player.m_PossessedEmtNameString;
            }
            else
            {
                m_PlayerNameTxt.text = "Unconnected";
                m_RoleTxt.text = "-";
                m_LocationTxt.text = "-";
            }
        }

        public void SetPlayer(NetworkConnection conn)
        {
            NetConnection = conn;

            if (conn != null)
                Player = conn.identity.GetComponent<E3DPlayer>();
        }
    }
}
