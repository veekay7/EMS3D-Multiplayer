using Mirror;
using UnityEngine;

namespace E3D
{
    public class E3DEvacOffrPlayer : E3DEmtPlayerBase
    {
        private EvacOffrUI m_myUI = null;
        [SerializeField, ReadOnlyVar]
        private AAmbulance m_ambulance = null;
        [SerializeField, ReadOnlyVar]
        protected AEvacPoint m_evacPoint = null;

        [SyncVar(hook = nameof(OnAmbulanceNetIdChanged)), ReadOnlyVar]
        public uint m_ambulanceNetId = 0;

        public event ListChangedFuncDelegate<AAmbulance> onAreaAmbulanceNumUpdateFunc;


        public AEvacPoint CurrentEvacPoint
        {
            get
            {
                if (isServer)
                {
                    if (m_curLocNetId == 0)
                    {
                        m_evacPoint = null;
                        return null;
                    }

                    if (m_evacPoint == null)
                        m_evacPoint = this.GetComponentFromNetId<AEvacPoint>(m_curLocNetId);

                    return m_evacPoint;
                }

                return m_evacPoint;
            }
        }

        public AAmbulance CurrentAmbulance
        {
            get
            {
                if (isServer)
                {
                    if (m_ambulanceNetId == 0)
                    {
                        m_ambulance = null;
                        return null;
                    }

                    if (m_ambulance == null)
                        m_ambulance = NetworkIdentity.spawned[m_ambulanceNetId].GetComponent<AAmbulance>();

                    return m_ambulance;
                }


                return m_ambulance;
            }
        }


        protected override void Awake()
        {
            base.Awake();
            m_useMoney = false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            onLocEnterExitFunc.AddListener(SetAmbulanceListChangedEvent);
        }

        protected override PlayerUIBase CreatePlayerUI()
        {
            GameObject newPlayerUIObject = Instantiate(m_PlayerUIPrefab);
            m_myUI = newPlayerUIObject.GetComponent<EvacOffrUI>();

            return m_myUI;
        }

        public override void SetLocation(AVictimPlaceableArea area)
        {
            base.SetLocation(area);

            if (m_curLocation != null)
                m_evacPoint = (AEvacPoint)m_curLocation;
        }

        private void SetAmbulanceListChangedEvent(AVictimPlaceableArea oldArea, AVictimPlaceableArea newArea)
        {
            if (oldArea != null)
                ((AEvacPoint)oldArea).ambulanceListChangedFunc -= Area_AmbulanceListChangedFunc;

            if (newArea != null)
                ((AEvacPoint)newArea).ambulanceListChangedFunc += Area_AmbulanceListChangedFunc;
        }

        private void Area_AmbulanceListChangedFunc(EListOperation op, AAmbulance oldItem, AAmbulance newItem)
        {
            if (onAreaAmbulanceNumUpdateFunc != null)
                onAreaAmbulanceNumUpdateFunc.Invoke(op, oldItem, newItem);
        }

        public void SetAmbulance(AAmbulance newAmbulance)
        {
            if (m_ambulance != null)
            {
                m_ambulance.Use(null);
                m_ambulance = null;
                CMD_SetAmbulance(null);
            }

            if (newAmbulance != null)
            {
                newAmbulance.Use(this);
                m_ambulance = newAmbulance;
                CMD_SetAmbulance(m_ambulance.netIdentity);
            }
        }

        [Command(requiresAuthority = false)]
        protected void CMD_SetAmbulance(NetworkIdentity ambulanceNetIdentity)
        {
            if (ambulanceNetIdentity != null)
                m_ambulanceNetId = ambulanceNetIdentity.netId;
            else
                m_ambulanceNetId = 0;
        }

        [Client]
        public void SetHospitalToAmbulance(Route hospitalRoute)
        {
            if (CurrentAmbulance == null)
                return;

            CurrentAmbulance.SetRoute(hospitalRoute);
        }

        [Client]
        public void LoadVictimToAmbulance(AVictim victim)
        {
            if (CurrentAmbulance == null)
                return;

            if (victim.IsPlayerUsing)
            {
                SendResponse("cannot_load_victim", victim.m_GivenName + " is in use.", null);
                return;
            }

            if (!victim.IsPlayerUsing && !victim.m_State.m_IsLoadedIntoVehicle)
            {
                // set flags for the vicitm
                victim.Cl_Use(this);

                // load the victim into the ambulance
                CurrentAmbulance.LoadVictim(victim);

                // remove the victim from the area
                CurrentEvacPoint.CMD_RemoveVictim(victim.netIdentity);

                SendResponse("ambulance_load_changed", victim.m_GivenName + " loaded into ambulance.", victim.netId.ToString());
            }
        }

        [Client]
        public void UnloadVictimFrmAmbulance()
        {
            if (CurrentAmbulance == null)
                return;

            if (CurrentAmbulance.Victim != null)
            {
                AVictim victim = CurrentAmbulance.Victim;

                // set the flags for the victim
                victim.Cl_Use(null);

                // unload the victim from the ambulance
                CurrentAmbulance.UnloadVictim(victim);

                // move the victim back to the evac point area
                CurrentEvacPoint.CMD_AddVictim(victim.netIdentity);

                SendResponse("ambulance_load_changed", victim.m_GivenName + " unloaded from ambulance.", victim.netId.ToString());
            }
        }

        [Client]
        public void EvacuateVictim()
        {
            if (CurrentAmbulance == null)
            {
                return;
            }

            if (CurrentAmbulance.Victim == null)
            {
                SendResponse("error", "No victim loaded in ambulance, cannot evacuate.", null);
            }
            else if (!CurrentAmbulance.HasRoute)
            {
                SendResponse("error", "Ambulance has no destination set.", null);
            }
            else
            {
                //Player.m_State.m_TotalVictimsAttendedNum++;

                CurrentAmbulance.CMD_Move(AAmbulance.EDirection.ToDestination);

                SendResponse("success", "Ambulance is now moving to hospital.", null);

                GameMode.Current.ProcessEvacScoring(this, m_ambulance);

                SetAmbulance(null);
            }
        }

        protected override void UpdateMapView()
        {
            var casualtyPoints = GameState.Current.m_CasualtyPoints.ToArray();
            var faps = GameState.Current.m_FirstAidPoints.ToArray();
            for (int i = 0; i < faps.Length; i++)
            {
                faps[i].SetVisible(false);
            }
            for (int i = 0; i < casualtyPoints.Length; i++)
            {
                casualtyPoints[i].SetVisible(false);
            }
        }

        private void OnAmbulanceNetIdChanged(uint oldId, uint newId)
        {
            if (isClient && !IsHumanPlayer)
            {
                if (m_ambulance != null && m_ambulance.netId == newId)
                    return;

                if (newId != 0)
                    m_ambulance = NetworkIdentity.spawned[newId].GetComponent<AAmbulance>();
            }
        }

        protected override void OnLocationNetIdChanged(uint oldId, uint newId)
        {
            base.OnLocationNetIdChanged(oldId, newId);

            if (isClient && !IsHumanPlayer)
            {
                if (m_curLocation != null)
                    m_evacPoint = (AEvacPoint)m_curLocation;
                else
                    m_evacPoint = null;
            }
        }

        public override void OnStopClient()
        {
            onLocEnterExitFunc.RemoveListener(SetAmbulanceListChangedEvent);
            base.OnStopClient();
        }
    }
}
