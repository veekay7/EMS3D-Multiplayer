using Mirror;
using UnityEngine;

namespace E3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(E3DPlayerState))]
    public class E3DPlayer : NetworkBehaviour, ICostable
    {
        public static KeyCode[] AlphaNumKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };

        [HideInInspector]
        public E3DPlayerState m_State;

        public GameObject m_DevConsoleWndPrefab = null;
        public GameObject m_PlayerUIPrefab = null;
        public int m_Cost = 0;
        public LayerMask m_RaycastIgnoreLayers;

        [SyncVar]
        public string m_PlayerName = "unnamed";

        // local vars
        protected bool m_useMoney = true;

        private GameCamera m_camera = null;
        private GUIController m_gameUI = null;
        private PlayerUIBase m_playerUI = null;

        [SyncVar, SerializeField]
        protected bool m_enableInput = false;
        [SyncVar]
        private bool m_readyToPlay = false;


        public static E3DPlayer Local { get; private set; }

        public GameCamera CurrentCamera { get => m_camera; }

        public bool InputEnabled { get => m_enableInput; }

        public bool IsReady { get => m_readyToPlay; }

        public bool CanMove { get; set; }

        public bool IsWaitingForPrompt { get; set; }

        public int Cost { get => m_Cost; }

        public virtual bool IsHumanPlayer { get => true; }


        protected virtual void Awake()
        {
            m_State = GetComponent<E3DPlayerState>();
        }

        protected virtual void Reset() { }

        public override void OnStartServer()
        {
            m_readyToPlay = false;
        }

        public override void OnStartClient()
        {
            m_State.SetPlayer(this);
            GameState.Current.AddPlayer(this);
        }

        private void Start()
        {
            if (isServer)
            {
                if (m_useMoney)
                    GameMode.Current.UseMoney(m_Cost);
            }
        }

        public override void OnStartLocalPlayer()
        {
            Local = this;

            m_camera = GameCamera.Current;
            m_gameUI = GUIController.Instance;

            CanMove = true;

            // setup the name
            CMD_SetPlayerName(GameCtrl.Instance.m_PlayerName);

            if (IsHumanPlayer)
            {
                InitPlayerUI();
                UpdateMapView();
                OpenBriefingScreen();
            }

            CMD_SetReady();

            //if (GameCtrl.Instance.mode == NetworkManagerMode.ClientOnly)
            //{
            //    var classSelectScreen = GUIController.Instance.m_CachedScreens[Consts.SCR_CLASS_SELECT_MENU];
            //    GUIController.Instance.OpenScreen(classSelectScreen);
            //}
            //GUIScreen waitScreen = m_gameUI.m_CachedScreens[Consts.SCR_PREMISSION_SCREEN];
            //m_gameUI.OpenScreen(waitScreen);
        }

        protected virtual void LateUpdate()
        {
            if (IsHumanPlayer)
            {
                if (isLocalPlayer)
                {
                    if (m_enableInput && !IsWaitingForPrompt)
                        HandleInput();
                }
            }
            else
            {
                if (GameCtrl.Instance.mode == NetworkManagerMode.Host)
                {
                    if (isServer && isClient)
                    {
                        if (m_enableInput)
                            Think();
                    }
                }
            }
        }

        protected virtual void HandleInput()
        {
            Input_BasicUI();
            Input_PlayerMove();
        }

        protected virtual void Think()
        {
            Cl_Think();
        }

        protected virtual void Cl_Think()
        {
        }

        protected void Input_BasicUI()
        {
            ///* mouse wheel */
            //float scrolledAmount = !Utils.Input_IsPointerOnGUI() ? (-Input.mouseScrollDelta.y * Mathf.Abs((float)Globals.m_GameConfig.m_ScrollSensitivity)) : 0.0f;
            //if (Mathf.Abs(scrolledAmount) > 0.0f)
            //{
            //}

            /* basic player input */
            if (Input.GetKeyDown(KeyCode.Tilde))
            {
                // TODO: open the console
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnEscKeyPressed();
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                OpenBriefingScreen();
            }
        }

        protected virtual void OnEscKeyPressed()
        {
            Pause();
        }

        protected void Input_PlayerMove()
        {
            /* movement input */
            if (CanMove)
            {
                float moveSpd = 3.0f;

                if (Input.GetKey(KeyCode.W))
                {
                    m_camera.transform.position += (transform.forward * moveSpd) * Time.deltaTime;
                }
                else if (Input.GetKey(KeyCode.S))
                {
                    m_camera.transform.position -= (transform.forward * moveSpd) * Time.deltaTime;
                }

                if (Input.GetKey(KeyCode.A))
                {
                    m_camera.transform.position -= (transform.right * moveSpd) * Time.deltaTime;
                }
                else if (Input.GetKey(KeyCode.D))
                {
                    m_camera.transform.position += (transform.right * moveSpd) * Time.deltaTime;
                }
            }
        }

        [Command]
        public void CMD_SetReady()
        {
            if (!m_readyToPlay)
            {
                m_readyToPlay = true;
            }
        }

        [Command(requiresAuthority = false)]
        public void CMD_SetPlayerName(string displayName)
        {
            m_PlayerName = displayName;
        }

        [Command(requiresAuthority = false)]
        public void CMD_EnableInput(bool value)
        {
            RPC_EnableInput(value);
        }

        [ClientRpc]
        public void RPC_EnableInput(bool value)
        {
            m_enableInput = value;
        }

        [Client]
        public virtual void Pause()
        {
            if (m_gameUI.ActiveScreen != null && GUIController.Instance.ActiveScreen.m_Classname == "Pause")
            {
                UnPause();
                return;
            }

            GUIScreen pauseScreen = m_gameUI.m_CachedScreens[Consts.SCR_PAUSE_SCREEN];
            m_gameUI.OpenScreen(pauseScreen);

            CMD_EnableInput(false);
        }

        [Client]
        public void UnPause()
        {
            if (m_gameUI.ActiveScreen != null && GUIController.Instance.ActiveScreen.m_Classname == "Pause")
                m_gameUI.CloseCurrentScreen();

            CMD_EnableInput(true);
        }

        [ClientRpc]
        public void RPC_GameOver()
        {
            if (isLocalPlayer)
                GameOver();
        }

        [Client]
        public void GameOver()
        {
            CanMove = false;
            CMD_EnableInput(false);

            m_playerUI.SetVisible(false);

            GUIScreen resultScreen = m_gameUI.m_CachedScreens[Consts.SCR_RESULT_SCREEN];
            m_gameUI.OpenScreen(resultScreen);
        }

        [Client]
        public void Disconnect()
        {
            CMD_EnableInput(false);

            if (GameCtrl.Instance != null)
            {
                GameCtrl.Instance.Disconnect();
            }
        }

        [Client]
        public void OpenBriefingScreen()
        {
            GUIScreen briefingScreen = m_gameUI.m_CachedScreens[Consts.SCR_BRIEFING_SCREEN];
            m_gameUI.OpenScreen(briefingScreen);

            CMD_EnableInput(false);
        }

        [Client]
        public void CloseBriefingScreen()
        {
            if (m_gameUI.ActiveScreen != null && GUIController.Instance.ActiveScreen.m_Classname == "Briefing")
                m_gameUI.CloseCurrentScreen();

            CMD_EnableInput(true);
        }

        protected virtual PlayerUIBase CreatePlayerUI()
        {
            return null;
        }

        protected void InitPlayerUI()
        {
            if (m_PlayerUIPrefab == null)
            {
                Debug.Log("m_PlayerUIPrefab is null");
                return;
            }

            m_playerUI = CreatePlayerUI();
            if (m_playerUI != null)
            {
                m_playerUI.SetPlayer(this);
                //m_hud.SetVisible(false);
            }
        }

        protected virtual void UpdateMapView() { }

        [Server]
        public virtual void SV_AssumeControl(GameObject oldPlayerObject)
        {
            if (oldPlayerObject == null)
                return;

            // copy the state over 
            E3DPlayer oldPlayer = oldPlayerObject.GetComponent<E3DPlayer>();
            E3DPlayerState oldPlayerState = oldPlayerObject.GetComponent<E3DPlayerState>();

            m_useMoney = false;

            m_State.m_TriageActionScore = oldPlayerState.m_TriageActionScore;
            m_State.m_TriageDmgScore = oldPlayerState.m_TriageDmgScore;
            m_State.m_CorrectTriageP3Num = oldPlayerState.m_CorrectTriageP3Num;
            m_State.m_CorrectTriageP2Num = oldPlayerState.m_CorrectTriageP2Num;
            m_State.m_CorrectTriageP1Num = oldPlayerState.m_CorrectTriageP1Num;
            m_State.m_CorrectTriageP0Num = oldPlayerState.m_CorrectTriageP0Num;
            m_State.m_UnderTriageNum = oldPlayerState.m_UnderTriageNum;
            m_State.m_OverTriageNum = oldPlayerState.m_OverTriageNum;
            m_State.m_TotalTriageTime = oldPlayerState.m_TotalTriageTime;

            m_State.m_WasDeadWhileInCare = oldPlayerState.m_WasDeadWhileInCare;
            m_State.m_TotalVictimsAttendedNum = oldPlayerState.m_TotalVictimsAttendedNum;
            m_State.m_TotalTreatmentNum = oldPlayerState.m_TotalTreatmentNum;
            m_State.m_CorrectTreatmentNum = oldPlayerState.m_CorrectTreatmentNum;
            m_State.m_TreatmentActionScore = oldPlayerState.m_TreatmentActionScore; ;
            m_State.m_TreatmentDmgScore = oldPlayerState.m_TreatmentDmgScore;
            m_State.m_TotalTreatmentTime = oldPlayerState.m_TotalTreatmentTime;

            m_State.m_EvacActionScore = oldPlayerState.m_EvacActionScore;
            m_State.m_EvacDmgScore = oldPlayerState.m_EvacDmgScore;

            // change the name to have the [BOT] prefix in the front
            m_PlayerName = "[BOT] " + oldPlayer.m_PlayerName;

            RPC_EnableInput(true);
        }

        public virtual void Cleanup()
        {
            Utils.SafeDestroyGameObject(m_playerUI);
        }

        public override void OnStopClient()
        {
            GameState.Current.RemovePlayer(this);

            if (m_playerUI != null)
                m_playerUI.UnsetPlayer();

            Cleanup();
        }

        protected virtual void OnDestroy() { }

        // editor only
        protected virtual void OnValidate()
        {
            m_State = gameObject.GetOrAddComponent<E3DPlayerState>();

            if (m_PlayerUIPrefab != null && m_PlayerUIPrefab.GetComponent<PlayerUIBase>() == null)
            {
                m_PlayerUIPrefab = null;
                Debug.LogError("m_PlayerUIPrefab is not of PlayerUIBase type.");
            }
        }
    }
}
