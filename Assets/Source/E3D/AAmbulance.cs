using Mirror;
using UnityEngine;

namespace E3D
{
    // TODO: 
    public class AAmbulance : NetworkBehaviour, ICostable
    {
        // type of ambulance
        public enum EType { Ambulance, PatientBus }

        // current state of the ambulance
        public enum EState { Idle, Moving }

        // the direction of travel
        public enum EDirection { None = -1, Returning = 0, ToDestination = 1 }

        // the type of the trip
        public enum ETripType { None, Single, RoundTrip }


        public string m_PrintName;
        public Sprite m_ThumbnailSprite;
        public int m_Cost;

        // network vars
        [SyncVar, SerializeField]
        private EState m_state;
        [SyncVar, SerializeField]
        private bool m_inUse;                            // is the ambulance in use by a player
        [SyncVar(hook = nameof(OnVictimIdChanged)), SerializeField]
        private uint m_victimNetId;

        // networked routing vars
        [SyncVar]
        public bool m_routeSet;
        [SyncVar(hook = nameof(OnEvacPointIdChanged)), SerializeField]
        private uint m_evacPointNetId;
        [SyncVar(hook = nameof(OnCurLocationIdChanged)), SerializeField]
        private uint m_curLocationNetId;
        [SyncVar(hook = nameof(OnStartIdChanged)), SerializeField]
        private uint m_startPointNetId;
        [SyncVar(hook = nameof(OnDestIdChanged)), SerializeField]
        private uint m_destinationNetId;
        [SyncVar, SerializeField]
        private float m_travelTime;
        [SyncVar, SerializeField]
        private float m_travelDist;

        [SyncVar, SerializeField]
        private float m_progress;
        //[SyncVar, SerializeField, ReadOnlyVar]
        //private ETripType m_tripType;                    // the type of the trip
        [SyncVar, SerializeField]
        private EDirection m_direction;                  // the current moving direction

        // local vars
        [SerializeField, ReadOnlyVar]
        private AVictim m_victim;

        // local routing vars
        [SerializeField, ReadOnlyVar]
        private AEvacPoint m_evacPoint;                  // the evac point the ambulance belongs to
        [SerializeField, ReadOnlyVar]
        private ALocationPoint m_currentLocation;
        [SerializeField, ReadOnlyVar]
        private ALocationPoint m_startPoint;
        [SerializeField, ReadOnlyVar]
        private ALocationPoint m_destination;
        //public event ListChangedFuncDelegate<AVictim> onVictimsChangedFunc;


        public int Cost { get => m_Cost; }

        public bool InUse { get => m_inUse; }

        public bool IsFull { get => m_victim != null; }

        public AVictim Victim
        {
            get
            {
                return m_victim;
            }
        }

        public bool HasRoute { get => m_routeSet; }

        public EState CurrentState { get => m_state; }

        public EDirection MovingDirection { get => m_direction; }

        public float Progress { get => m_progress; }

        public AEvacPoint EvacPoint
        {
            get
            {
                return m_evacPoint;
            }
        }

        public ALocationPoint StartPoint
        {
            get
            {
                return m_startPoint;
            }
        }

        public ALocationPoint Destination
        {
            get
            {
                return m_destination;
            }
        }

        public float TravelTime { get => m_travelTime; }

        public float TravelDistance { get => m_travelDist; }


        private void Awake()
        {
            m_state = EState.Idle;
            m_inUse = false;

            m_routeSet = false;
            m_evacPoint = null;
            m_currentLocation = null;
            m_startPoint = null;
            m_destination = null;
            m_progress = 0.0f;
        }

        private void Reset()
        {
            m_PrintName = "Unknown";
            //m_Type = EType.Ambulance;
            //m_MaxCapacity = 1;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        public override void OnStartClient()
        {
            GameState.Current.AddAmbulance(this);
        }

        private void Start()
        {
            if (isServer)
                GameMode.Current.UseMoney(m_Cost);
        }

        private void Update()
        {
            if (isServer)
            {
                m_routeSet = (m_startPointNetId != 0 && m_destinationNetId != 0) ? true : false;

                // only if route is set than you can do moving logic!!
                if (m_routeSet)
                {
                    if (m_state == EState.Moving)
                    {
                        if (m_progress < m_travelTime)
                        {
                            m_progress += Time.deltaTime;
                            return;
                        }
                        else
                        {
                            m_progress = 0.0f;

                            if (Destination != null && Victim != null)
                            {
                                Debug.Log("Arrived at destination " + Destination.m_LocationId + " with victim " + Victim.m_GivenName);
                                GameState.Current.m_NumEvacuated++;
                            }

                            //RPC_OnArriveAtDestination();

                            if (Destination is AHospital)
                            {
                                if (Victim != null)
                                {
                                    Victim.m_State.CMD_SetEvacuatedFlag(true);

                                    // send to hospital
                                    AHospital hospital = (AHospital)Destination;
                                    hospital.AddVictim(Victim);

                                    // unload them victims
                                    //UnloadVictim(Victim);
                                    m_victimNetId = 0;

                                    //// NOTE: moved to hospital AddVictim()!!
                                    //GameState.Current.CMD_UpdateEvacScore();
                                }

                                // set the current location
                                m_curLocationNetId = Destination.netId;
                                //SetCurrentLocation(Destination);

                                //SetRoute(Destination, StartPoint, m_travelTime, m_travelDist);
                                SV_SetRoute(Destination.netIdentity, StartPoint.netIdentity, m_travelTime, m_travelDist);
                                SV_Move(EDirection.Returning);
                            }
                            else if (Destination is AEvacPoint)
                            {
                                // set the current location
                                //SetCurrentLocation(Destination);
                                m_curLocationNetId = Destination.netId;

                                SV_Stop();

                                // clear route bish1
                                SV_SetRoute(null, null, 0.0f, 0.0f);
                                m_direction = EDirection.None;
                                m_inUse = false;
                            }
                        }
                    }
                }
            }
        }

        [ClientRpc]
        private void RPC_OnArriveAtDestination()
        {
            Debug.Log("REEEEEEE: Called RPC_OnArriveAtDest()");

            // if reached the hospital, unload the patient in the hospital if there is a patient inside
            // if reached the evac point, don't do anything
            if (Destination is AHospital)
            {
                if (Victim != null)
                {
                    Victim.m_State.CMD_SetEvacuatedFlag(true);

                    AVictim victim = Victim;

                    // the sequence must be like such!!
                    // unload them victims
                    UnloadVictim(Victim);

                    // send to hospital
                    AHospital hospital = (AHospital)Destination;
                    hospital.AddVictim(victim);

                    //// NOTE: moved to hospital AddVictim()!!
                    //GameState.Current.CMD_UpdateEvacScore();
                }

                // set the current location
                SetCurrentLocation(Destination);

                SetRoute(Destination, StartPoint, m_travelTime, m_travelDist);
                CMD_Move(EDirection.Returning);
            }
            else if (Destination is AEvacPoint)
            {
                // set the current location
                SetCurrentLocation(Destination);

                CMD_Stop();
                CMD_ClearRoute();
            }
        }


        #region Location Management

        [Server]
        public void SV_SetEvacPoint(AEvacPoint evacPoint, bool setAsCurrent = false)
        {
            if (evacPoint != null)
                RPC_SetEvacPoint(evacPoint.netId, setAsCurrent);
        }

        [ClientRpc]
        private void RPC_SetEvacPoint(uint evacPointNetId, bool setAsCurrent)
        {
            NetworkIdentity identity = NetworkIdentity.spawned[evacPointNetId];
            AEvacPoint evacPoint = identity.GetComponent<AEvacPoint>();
            SetEvacPoint(evacPoint, setAsCurrent);
        }

        [Client]
        public void SetEvacPoint(AEvacPoint evacPoint, bool setAsCurrent = false)
        {
            if (evacPoint != null)
            {
                m_evacPoint = evacPoint;
                CMD_SetEvacPoint(m_evacPoint.netIdentity);

                if (setAsCurrent)
                {
                    SetCurrentLocation(m_evacPoint);
                }
            }
            else
            {
                m_currentLocation = null;
            }
        }

        [Command(requiresAuthority = false)]
        private void CMD_SetEvacPoint(NetworkIdentity evacPtNetIdentity)
        {
            if (evacPtNetIdentity == null)
                m_evacPointNetId = 0;
            else
                m_evacPointNetId = evacPtNetIdentity.netId;
        }

        [Client]
        public void SetCurrentLocation(ALocationPoint newCurLocation)
        {
            //m_currentLocation = newCurLocation;

            if (newCurLocation != null)
                CMD_SetCurLocation(newCurLocation.netIdentity);
            else
                CMD_SetCurLocation(null);
        }

        [Command(requiresAuthority = false)]
        private void CMD_SetCurLocation(NetworkIdentity newcurLocNetIdentity)
        {
            if (newcurLocNetIdentity == null)
                m_curLocationNetId = 0;
            else
                m_curLocationNetId = newcurLocNetIdentity.netId;
        }

        [ClientRpc]
        private void RPC_SetCurrentLocation(NetworkIdentity newCurLocNetIdentity)
        {
            if (newCurLocNetIdentity != null)
            {
                ALocationPoint newCurLoc = newCurLocNetIdentity.GetComponent<ALocationPoint>();
                m_currentLocation = newCurLoc;
            }
            else
            {
                m_currentLocation = null;
            }
        }

        #endregion


        #region Routing

        [Client]
        public void SetRoute(Route newRoute)
        {
            SetRoute(m_currentLocation, newRoute.m_Location, newRoute.m_TravelTime, newRoute.m_Distance);
        }

        [Client]
        public void SetRoute(ALocationPoint start, ALocationPoint destination, float travelTime, float distance)
        {
            //m_startPoint = start;
            //m_destination = destination;
            //m_travelTime = travelTime;
            //m_travelDist = distance;

            CMD_SetRoute(start.netIdentity, destination.netIdentity, travelTime, distance);

            m_direction = EDirection.None;
        }

        [Command(requiresAuthority = false)]
        public void CMD_ClearRoute()
        {
            //CMD_SetRoute(null, null, 0.0f, 0.0f);
            SV_SetRoute(null, null, 0.0f, 0.0f);
            m_direction = EDirection.None;
            m_inUse = false;
        }

        [Command(requiresAuthority = false)]
        private void CMD_SetRoute(NetworkIdentity start, NetworkIdentity dest, float travelTime, float travelDist)
        {
            //Debug.Log("Reeeeeeee: "+ start + " " + dest);
            SV_SetRoute(start, dest, travelTime, travelDist);
        }

        [Server]
        private void SV_SetRoute(NetworkIdentity start, NetworkIdentity dest, float travelTime, float travelDist)
        {
            m_startPointNetId = start != null ? start.netId : 0;
            m_destinationNetId = dest != null ? dest.netId : 0;
            m_travelTime = travelTime;
            m_travelDist = travelDist;
        }

        private void OnEvacPointIdChanged(uint oldId, uint newId)
        {
            if (oldId != newId)
            {
                if (newId == 0)
                {
                    m_evacPoint = null;
                    return;
                }

                m_evacPoint = this.GetComponentFromNetId<AEvacPoint>(newId);
            }
        }

        private void OnCurLocationIdChanged(uint oldId, uint newId)
        {
            if (oldId != newId)
            {
                if (newId == 0)
                {
                    m_currentLocation = null;
                    return;
                }

                m_currentLocation = this.GetComponentFromNetId<ALocationPoint>(newId);
            }
        }

        private void OnStartIdChanged(uint oldId, uint newId)
        {
            if (oldId != newId)
            {
                if (newId == 0)
                {
                    m_startPoint = null;
                    return;
                }

                m_startPoint = this.GetComponentFromNetId<ALocationPoint>(newId);
            }
        }

        private void OnDestIdChanged(uint oldId, uint newId)
        {
            if (oldId != newId)
            {
                if (newId == 0)
                {
                    m_destination = null;
                    return;
                }

                m_destination = this.GetComponentFromNetId<ALocationPoint>(newId);
            }
        }

        #endregion


        #region Move/Stop Ambulance

        [Command(requiresAuthority = false)]
        public void CMD_Move(EDirection direction)
        {
            SV_Move(direction);
        }

        [Command(requiresAuthority = false)]
        public void CMD_Stop()
        {
            SV_Stop();
        }

        [Server]
        public void SV_Move(EDirection direction)
        {
            m_direction = direction;
            //m_tripType = tripType;
            m_state = EState.Moving;
        }

        [Server]
        public void SV_Stop()
        {
            //m_tripType = ETripType.None;
            m_routeSet = false;
            m_state = EState.Idle;
        }

        #endregion


        #region Player Functions

        [Client]
        public void Use(E3DPlayer player)
        {
            if (player == null)
                CMD_Use(null);
            else
                CMD_Use(player.netIdentity);
        }

        [Command(requiresAuthority = false)]
        private void CMD_Use(NetworkIdentity playerNetIdentity)
        {
            if(playerNetIdentity != null)
            {
                m_inUse = true;
            }
        }

        #endregion


        #region Victim functions

        [Client]
        public void LoadVictim(AVictim newVictim)
        {
            if (newVictim == null)
            {
                return;
            }

            CMD_SetVictim(newVictim.netIdentity);
            //m_victim = newVictim;
            //m_victim.m_State.m_IsLoadedIntoVehicle = true;
        }

        [Client]
        public void UnloadVictim(AVictim victim)
        {
            if (m_victim == victim)
            {
                //m_victim.m_State.m_IsLoadedIntoVehicle = false;
                //m_victim = null;
                CMD_SetVictim(null);
            }
        }

        [Command(requiresAuthority = false)]
        private void CMD_SetVictim(NetworkIdentity victimNetIdentity)
        {
            if (victimNetIdentity != null)
                m_victimNetId = victimNetIdentity.netId;
            else
                m_victimNetId = 0;
        }

        private void OnVictimIdChanged(uint oldId, uint newId)
        {
            if (oldId != newId)
            {
                if (newId == 0)
                {
                    if (m_victim != null)
                    {
                        m_victim.m_State.m_IsLoadedIntoVehicle = false;
                        m_victim = null;
                    }

                    return;
                }

                m_victim = this.GetComponentFromNetId<AVictim>(newId);
                if (m_victim != null)
                    m_victim.m_State.m_IsLoadedIntoVehicle = true;
            }
        }

        public bool ContainsVictim(AVictim victim)
        {
            if (m_victim == victim)
                return true;

            return false;
        }

        [Server]
        public bool SV_ContainsVictim(NetworkIdentity victimNetIdentity)
        {
            if (m_victimNetId == 0)
                return false;

            return NetworkIdentity.spawned[m_victimNetId] == victimNetIdentity;
        }

        #endregion


        public override void OnStopClient()
        {
            GameState.Current.RemoveAmbulance(this);
        }
    }
}
