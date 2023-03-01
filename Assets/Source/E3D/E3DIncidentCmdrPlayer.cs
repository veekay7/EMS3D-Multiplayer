using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace E3D
{
    public class E3DIncidentCmdrPlayer : E3DEmtPlayerBase
    {
        public GameObject m_buildProxyPrefab;
        public List<ItemAttrib> m_Items = new List<ItemAttrib>();

        [SyncVar, SerializeField, ReadOnlyVar]
        private int m_numItemBags = 10;
        [SerializeField, ReadOnlyVar]
        private SyncList<int> m_itemQuantitiesSync = new SyncList<int>();

        private IncidentCmdrUI m_myUI = null;
        [SerializeField, ReadOnlyVar]
        private AVictimPlaceableArea m_selectedLocation = null;

        private GameObject m_buildProxy;
        private GameObject m_buildPrefab;
        private BoxCollider m_buildProxyBoxCollider;

        public SyncList<int> ItemQuantities
        {
            get => m_itemQuantitiesSync;
        }

        public bool PlacementMode { get; private set; }


        public override void OnStartServer()
        {
            // set initial amount of items first
            for (int itemSlot = 0; itemSlot < m_Items.Count; itemSlot++)
            {
                if (m_Items[itemSlot] != null)
                {
                    if (!m_Items[itemSlot].m_IsInfinite)
                    {
                        int newAmount = m_Items[itemSlot].m_DefaultCarry * m_numItemBags;
                        m_itemQuantitiesSync.Add(newAmount);
                    }
                    else
                    {
                        m_itemQuantitiesSync.Add(Consts.ITEM_INFINITE);
                    }
                }
                else
                {
                    m_itemQuantitiesSync[itemSlot] = Consts.ITEM_EMPTY_SLOT;
                }
            }

            base.OnStartServer();
        }

        private void Start()
        {
            // calculate cost and shit and put it on the damn game mode
            if (isServer)
            {
                GameMode.Current.CalcMoneyGiven(GameCtrl.Instance.m_Difficulty);
                for (int itemSlot = 0; itemSlot < m_Items.Count; itemSlot++)
                {
                    if (m_itemQuantitiesSync[itemSlot] != Consts.ITEM_INFINITE)
                    {
                        if (isServer)
                            GameMode.Current.UseMoney(m_Items[itemSlot].Cost * m_itemQuantitiesSync[itemSlot]);
                    }
                }
            }
        }

        protected override void HandleInput()
        {
            base.HandleInput();

            // placement modeinptu
            if (PlacementMode)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    StopBuildMode();
                    return;
                }

                Ray ray = CurrentCamera.m_CameraComponent.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit1, float.MaxValue))
                {
                    m_buildProxy.transform.position = hit1.point;

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (Physics.Raycast(ray, out RaycastHit hit2, float.MaxValue, ~m_RaycastIgnoreLayers))
                        {
                            // check if we are pointing in a placement area or not
                            if (hit2.transform.gameObject.GetComponent<APlacementArea>())
                            {
                                // check if proxy has no collision or some shit!!
                                bool canPlace = true;
                                Collider[] collisions = Physics.OverlapBox(m_buildProxy.transform.position + m_buildProxyBoxCollider.center, m_buildProxyBoxCollider.size / 2.0f, Quaternion.identity);
                                for (int i = 0; i < collisions.Length; i++)
                                {
                                    if (collisions[i].gameObject.GetComponent<AVictimPlaceableArea>())
                                    {
                                        canPlace = false;
                                        break;
                                    }
                                }

                                if (canPlace)
                                {
                                    // plot that shit here!
                                    GameObject newBuildingObject = Instantiate(m_buildPrefab);
                                    newBuildingObject.transform.position = hit2.point;

                                    NetworkServer.Spawn(newBuildingObject);

                                    // exit build mode
                                    StopBuildMode();
                                }
                            }
                        }
                    }
                }

                return;
            }
            // end placement mode logic

            // not in placaement mode
            if (Input.GetMouseButtonDown(0))
            {
                // if mouse if over the a GUI object or there is a prompt waiting, don't bother raycasting and do any shit
                if (Utils.Input_IsPointerOnGUI() || IsWaitingForPrompt)
                    return;

                Ray ray = CurrentCamera.m_CameraComponent.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, ~m_RaycastIgnoreLayers))
                {
                    GameObject hitObject = hit.transform.gameObject;
                    var hitArea = hitObject.GetComponent<AVictimPlaceableArea>();
                    if (hitArea != null)
                    {
                        m_selectedLocation = hitArea;
                        m_myUI.OnAreaSelected(m_selectedLocation);
                    }
                }
                else
                {
                    m_myUI.OnAreaDeselected();
                }
            }
        }

        public override void Pause()
        {
            if (PlacementMode)
                return;

            base.Pause();
        }

        protected override void OnEscKeyPressed()
        {
            if (PlacementMode)
            {
                // if esc key is pressed, go back to the fucking start page
                StopBuildMode();
                return;
            }

            Pause();
        }

        [Client]
        public void StartGame()
        {
            if (GameState.Current.m_CurrentMatchState == EMatchState.WaitingToStart)
            {
                GameMode.Current.CMD_StartGame();
            }
        }

        [Client]
        public void StartBuildMode(GameObject buildPrefab)
        {
            if (buildPrefab.GetComponent<AEvacPoint>() != null || buildPrefab.GetComponent<AFirstAidPoint>() != null)
            {
                PlacementMode = true;

                m_buildPrefab = buildPrefab;

                if (m_buildProxy == null)
                    m_buildProxy = Instantiate(m_buildProxyPrefab);

                m_buildProxy.SetActive(true);

                m_buildProxy.GetComponent<SpriteRenderer>().sprite =
                    m_buildPrefab.GetComponent<SpriteRenderer>().sprite;

                m_buildProxyBoxCollider = m_buildProxy.GetComponent<BoxCollider>();

                m_buildProxyBoxCollider.center = m_buildPrefab.GetComponent<BoxCollider>().center;
                m_buildProxyBoxCollider.size =
                    m_buildPrefab.GetComponent<BoxCollider>().size;

                // show all placement areas
                var placementAreas = APlacementArea.placementAreas;
                for (int i = 0; i < placementAreas.Count; i++)
                {
                    placementAreas[i].SetVisible(true);
                }

                if (m_myUI != null)
                {
                    //m_myUI.SetVisible(false);
                    m_myUI.OpenView(2);
                }
            }
        }

        [Client]
        public void StopBuildMode()
        {
            PlacementMode = false;

            m_buildPrefab = null;
            m_buildProxyBoxCollider = null;

            m_buildProxy.SetActive(false);

            // hide all placement areas
            var placementAreas = APlacementArea.placementAreas;
            for (int i = 0; i < placementAreas.Count; i++)
            {
                placementAreas[i].SetVisible(false);
            }

            if (m_myUI != null)
            {
                m_myUI.OpenView(0);
                //m_myUI.SetVisible(true);
            }
        }

        [Command]
        public void CMD_AddItemQuantity(int itemIndex, int amount)
        {
            m_itemQuantitiesSync[itemIndex] += amount;
            if (m_itemQuantitiesSync[itemIndex] > Consts.ABS_MAX_ITEMS)
                m_itemQuantitiesSync[itemIndex] = 0;
        }

        [Command]
        public void CMD_RemoveItemQuantity(int itemIndex, int amount)
        {
            m_itemQuantitiesSync[itemIndex] -= amount;
            if (m_itemQuantitiesSync[itemIndex] < 0)
                m_itemQuantitiesSync[itemIndex] = 0;
        }

        [Client]
        public void SendAmbulanceToEvacPoint(AAmbulanceDepot depot, AEvacPoint evacPoint)
        {
            depot.SendAmbulanceTo(evacPoint);
            SendResponse("send_ambulance", "Deployed ambulance from " + depot.m_PrintName + " to " + evacPoint.m_PrintName + ".", null);
        }

        protected override PlayerUIBase CreatePlayerUI()
        {
            GameObject newPlayerUIObject = Instantiate(m_PlayerUIPrefab);
            m_myUI = newPlayerUIObject.GetComponent<IncidentCmdrUI>();

            return m_myUI;
        }
    }
}
