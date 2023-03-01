using UnityEngine;

namespace E3D
{
    public class PauseScreen : GUIScreen
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReturnToGame();
            }
        }

        public void ReturnToGame()
        {
            if (E3DPlayer.Local != null)
                E3DPlayer.Local.UnPause();
        }

        public void DisconnectFromGame()
        {
            if (GUIController.Instance.ActiveScreen == this)
                GUIController.Instance.CloseCurrentScreen();

            if (E3DPlayer.Local != null)
                E3DPlayer.Local.Disconnect();
        }
    }
}
