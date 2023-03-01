using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace E3D
{
    public class AEvacPoint : AVictimPlaceableArea, ICostable
    {
        [Header("Evac Point")]
        public int m_Cost = 0;
        public int m_MaxCapacity = 10;

        // local vars
        [SerializeField, ReadOnlyVar]
        private List<AAmbulance> m_ambulances = new List<AAmbulance>();

        // network vars
        [ReadOnlyVar]
        public SyncList<uint> m_ambulanceNetIds = new SyncList<uint>();

        public event ListChangedFuncDelegate<AAmbulance> ambulanceListChangedFunc;


        public int NumAmbulances { get => m_ambulances.Count; }
        
        public int Cost { get => m_Cost; }


        protected override void Awake()
        {
            base.Awake();
            MultiplePlayersAllowed = false;
            VehicleCanPass = true;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            m_ambulanceNetIds.Callback += Callback_AmbulanceNetIds;

            if (E3DPlayer.Local != null)
            {
                if (!(E3DPlayer.Local is E3DEvacOffrPlayer) && !(E3DPlayer.Local is E3DIncidentCmdrPlayer))
                    SetVisible(false);
            }
        }

        private void Start()
        {
            if (isServer)
                GameMode.Current.UseMoney(m_Cost);
        }

        [Server]
        public void SV_AddAmbulance(AAmbulance newAmbulance)
        {
            if (!m_ambulanceNetIds.Contains(newAmbulance.netId))
            {
                m_ambulanceNetIds.Add(newAmbulance.netId);
            }
        }

        [Server]
        public void SV_RemoveAmbulance(AAmbulance ambulance)
        {
            if (m_ambulanceNetIds.Contains(ambulance.netId))
            {
                m_ambulanceNetIds.Remove(ambulance.netId);
            }
        }

        [Client]
        public void AddAmbulance(AAmbulance newAmbulance)
        {
            if (!m_ambulances.Contains(newAmbulance))
            {
                m_ambulances.Add(newAmbulance);

                if (ambulanceListChangedFunc != null)
                    ambulanceListChangedFunc.Invoke(EListOperation.Add, null, newAmbulance);
            }
        }

        [Client]
        public void RemoveAmbulance(AAmbulance ambulance)
        {
            if (m_ambulances.Contains(ambulance))
            {
                m_ambulances.Remove(ambulance);
                if (ambulanceListChangedFunc != null)
                    ambulanceListChangedFunc.Invoke(EListOperation.Remove, ambulance, null);
            }
        }

        [Command(requiresAuthority = false)]
        public void CMD_AddAmbulance(NetworkIdentity newAmbulanceNetIdentity)
        {
            if (newAmbulanceNetIdentity == null)
            {
                Debug.LogError("newAmbulanceNetIdentity is null");
                return;
            }

            AAmbulance newAmbulance = newAmbulanceNetIdentity.GetComponent<AAmbulance>();
            SV_AddAmbulance(newAmbulance);
        }

        [Command(requiresAuthority = false)]
        public void CMD_RemoveAmbulance(NetworkIdentity ambulanceNetIdentity)
        {
            if (ambulanceNetIdentity == null)
            {
                Debug.LogError("victimNetIdentity is null");
                return;
            }

            AAmbulance ambulance = ambulanceNetIdentity.GetComponent<AAmbulance>();
            SV_RemoveAmbulance(ambulance);
        }

        public AAmbulance[] GetAmbulances()
        {
            return m_ambulances.ToArray();
        }

        private void Callback_AmbulanceNetIds(SyncList<uint>.Operation op, int itemIndex, uint oldItem, uint newItem)
        {
            if (op == SyncList<uint>.Operation.OP_ADD)
            {
                var newAmbulance = NetworkIdentity.spawned[newItem].GetComponent<AAmbulance>();
                AddAmbulance(newAmbulance);
            }
            else if (op == SyncList<uint>.Operation.OP_REMOVEAT)
            {
                var ambulance = NetworkIdentity.spawned[oldItem].GetComponent<AAmbulance>();
                RemoveAmbulance(ambulance);
            }
        }
    }
}
