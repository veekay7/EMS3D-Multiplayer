using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace E3D
{
    public class GameState : NetworkBehaviour
    {
        [SyncVar]
        public EMatchState m_CurrentMatchState;
        [SyncVar]
        public EMatchState m_LastMatchState;
        [SyncVar]
        public int m_NumTriageCorrect;
        [SyncVar]
        public int m_NumTriageOver;
        [SyncVar]
        public int m_NumTriageUnder;
        [SyncVar]
        public int m_NumTriaged;
        [SyncVar]
        public int m_NumTreated;
        [SyncVar]
        public int m_NumEvacuated;
        [SyncVar]
        public int m_NumDead;

        public ARouteController m_RouteController;
        public List<E3DPlayer> m_Players = new List<E3DPlayer>();
        public List<AVictim> m_Victims = new List<AVictim>();
        public List<AAmbulance> m_Ambulances = new List<AAmbulance>();

        public List<AVictimPlaceableArea> m_CasualtyPoints = new List<AVictimPlaceableArea>();
        public List<AFirstAidPoint> m_FirstAidPoints = new List<AFirstAidPoint>();
        public List<AEvacPoint> m_EvacPoints = new List<AEvacPoint>();
        public List<AHospital> m_Hospitals = new List<AHospital>();
        public List<AAmbulanceDepot> m_AmbulanceDepots = new List<AAmbulanceDepot>();
        public List<APlacementArea> m_PlacementAreas = new List<APlacementArea>();

        public List<E3DTriageOffrPlayer> m_TriageOfficers = new List<E3DTriageOffrPlayer>();
        public List<E3DFirstAidDocPlayer> m_FirstAidDocs = new List<E3DFirstAidDocPlayer>();
        public List<E3DEvacOffrPlayer> m_EvacOfficers = new List<E3DEvacOffrPlayer>();

        public static GameState Current { get; private set; }


        private void Awake()
        {
            if (Current != null && Current == this)
                return;

            Current = this;
        }

        [Command(requiresAuthority = false)]
        public void CMD_UpdateTriageScore(uint victimNetId)
        {
            if (victimNetId == 0)
            {
                Debug.Log("cannot process game triage scoring, victimNetId is 0");
                return;
            }

            AVictim victim = NetworkIdentity.spawned[victimNetId].GetComponent<AVictim>();
            EPACS currentPAC = GameMode.CheckVictimPAC(victim);
            
            if (currentPAC == victim.m_State.m_GivenPACS)
            {
                // correct triage for mission report
                m_NumTriageCorrect++;
            }
            else
            {
                int pac_comp = GameMode.ComparePACS(victim.m_State.m_GivenPACS, currentPAC, out int diff);
                if (pac_comp == 1)
                {
                    // over triaged
                    m_NumTriageOver++;
                }
                else if (pac_comp == -1)
                {
                    // under triaged
                    m_NumTriageUnder++;
                }
            }

            m_NumTriaged += 1;
        }

        [Command(requiresAuthority = false)]
        public void CMD_UpdateTreatmentScore()
        {
            // do the tally
            m_NumTreated += 1;
        }

        [Command(requiresAuthority = false)]
        public void CMD_UpdateMorgueScore()
        {
            // do the tally
            m_NumTreated += 1;
            m_NumDead += 1;
        }

        [Command(requiresAuthority = false)]
        public void CMD_UpdateEvacScore()
        {
            // do the tally
            m_NumEvacuated++;
        }

        public void AddPlayer(E3DPlayer player)
        {
            if (!m_Players.Contains(player))
            {
                m_Players.Add(player);
            }
        }

        public void RemovePlayer(E3DPlayer player)
        {
            if (m_Players.Contains(player))
            {
                m_Players.Remove(player);
            }
        }

        public void AddVictim(AVictim victim)
        {
            if (!m_Victims.Contains(victim))
            {
                m_Victims.Add(victim);
            }
        }

        public void RemoveVictim(AVictim victim)
        {
            if (m_Victims.Contains(victim))
            {
                m_Victims.Remove(victim);
            }
        }

        public void AddLocation(ALocationPoint location)
        {
            if (location is AVictimPlaceableArea)
            {
                AVictimPlaceableArea area = (AVictimPlaceableArea)location;

                if (area is AFirstAidPoint)
                {
                    if (!m_FirstAidPoints.Contains((AFirstAidPoint)location))
                        m_FirstAidPoints.Add((AFirstAidPoint)location);
                }
                else if (area is AEvacPoint)
                {
                    if (!m_EvacPoints.Contains((AEvacPoint)location))
                        m_EvacPoints.Add((AEvacPoint)location);
                }
                else
                {
                    if (!m_CasualtyPoints.Contains(area))
                        m_CasualtyPoints.Add(area);
                }
            }
            else if (location is AHospital)
            {
                if (!m_Hospitals.Contains((AHospital)location))
                    m_Hospitals.Add((AHospital)location);
            }
            else if (location is AAmbulanceDepot)
            {
                if (!m_AmbulanceDepots.Contains((AAmbulanceDepot)location))
                    m_AmbulanceDepots.Add((AAmbulanceDepot)location);
            }
        }

        public void RemoveLocation(ALocationPoint location)
        {
            if (location is AVictimPlaceableArea)
            {
                AVictimPlaceableArea area = (AVictimPlaceableArea)location;

                if (area is AFirstAidPoint)
                {
                    if (m_FirstAidPoints.Contains((AFirstAidPoint)area))
                        m_FirstAidPoints.Remove((AFirstAidPoint)area);
                }
                else if (area is AEvacPoint)
                {
                    if (m_EvacPoints.Contains((AEvacPoint)area))
                        m_EvacPoints.Remove((AEvacPoint)area);
                }
                else
                {
                    if (m_CasualtyPoints.Contains(area))
                        m_CasualtyPoints.Remove(area);
                }
            }
            else if (location is AHospital)
            {
                if (m_Hospitals.Contains((AHospital)location))
                    m_Hospitals.Remove((AHospital)location);
            }
            else if (location is AAmbulanceDepot)
            {
                if (m_AmbulanceDepots.Contains((AAmbulanceDepot)location))
                    m_AmbulanceDepots.Remove((AAmbulanceDepot)location);
            }
        }

        public void AddAmbulance(AAmbulance newAmbulance)
        {
            if (!m_Ambulances.Contains(newAmbulance))
                m_Ambulances.Add(newAmbulance);
        }

        public void RemoveAmbulance(AAmbulance ambulance)
        {
            if (m_Ambulances.Contains(ambulance))
                m_Ambulances.Remove(ambulance);
        }

        public void AddEmtActor(E3DEmtPlayerBase emt)
        {
            if (emt is E3DTriageOffrPlayer)
            {
                if (!m_TriageOfficers.Contains((E3DTriageOffrPlayer)emt))
                {
                    m_TriageOfficers.Add((E3DTriageOffrPlayer)emt);
                }
            }
            else if (emt is E3DFirstAidDocPlayer)
            {
                if (!m_FirstAidDocs.Contains((E3DFirstAidDocPlayer)emt))
                {
                    m_FirstAidDocs.Add((E3DFirstAidDocPlayer)emt);
                }
            }
            else if (emt is E3DEvacOffrPlayer)
            {
                if (!m_EvacOfficers.Contains((E3DEvacOffrPlayer)emt))
                {
                    m_EvacOfficers.Add((E3DEvacOffrPlayer)emt);
                }
            }
        }

        public void RemoveEmtActor(E3DEmtPlayerBase officer)
        {
            if (officer is E3DTriageOffrPlayer)
            {
                if (m_TriageOfficers.Contains((E3DTriageOffrPlayer)officer))
                {
                    m_TriageOfficers.Remove((E3DTriageOffrPlayer)officer);
                }
            }
            else if (officer is E3DFirstAidDocPlayer)
            {
                if (m_FirstAidDocs.Contains((E3DFirstAidDocPlayer)officer))
                {
                    m_FirstAidDocs.Remove((E3DFirstAidDocPlayer)officer);
                }
            }
            else if (officer is E3DEvacOffrPlayer)
            {
                if (m_EvacOfficers.Contains((E3DEvacOffrPlayer)officer))
                {
                    m_EvacOfficers.Remove((E3DEvacOffrPlayer)officer);
                }
            }
        }

        public void Clear()
        {
            //m_GameType = EGameType.Singleplay;
            m_CurrentMatchState = EMatchState.Enter;
            m_LastMatchState = EMatchState.Enter;

            m_NumTriageCorrect = 0;
            m_NumTriageOver = 0;
            m_NumTriageUnder = 0;
            m_NumTriaged = 0;
            m_NumTreated = 0;
            m_NumEvacuated = 0;
            m_NumDead = 0;

            m_Victims.Clear();

            m_AmbulanceDepots.Clear();
            m_Hospitals.Clear();
            m_CasualtyPoints.Clear();
            m_FirstAidPoints.Clear();
            m_EvacPoints.Clear();

            m_TriageOfficers.Clear();
            m_FirstAidDocs.Clear();
            m_EvacOfficers.Clear();
        }

        public override void OnStopClient()
        {
            Clear();
        }

        [ClientRpc]
        private void RPC_DebugLog(string msg)
        {
            Debug.Log(msg);
        }
    }
}
