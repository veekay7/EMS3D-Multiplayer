using Mirror;
using UnityEngine;

namespace E3D
{
    public class AAmbulanceDepot : ALocationPoint
    {
        public const int MinAmbulances = 6;
        public const int MaxAmbulances = 12;

        [SyncVar, SerializeField, ReadOnlyVar]
        private int m_numAmbulancesLeft = 0;


        public int NumAmbulancesLeft
        {
            get => m_numAmbulancesLeft;
        }


        public override void OnStartServer()
        {
            m_numAmbulancesLeft = UnityEngine.Random.Range(MinAmbulances, MaxAmbulances + 1);
            base.OnStartServer();
        }

        public void SendAmbulanceTo(AEvacPoint evacPoint)
        {
            CMD_ReduceNumAmbulancesLeft();
            CMD_CreateAmbulance(evacPoint.netIdentity);
        }

        [Command(requiresAuthority = false)]
        private void CMD_CreateAmbulance(NetworkIdentity evacPointNetIdentity)
        {
            AEvacPoint evacPoint = evacPointNetIdentity.GetComponent<AEvacPoint>();

            GameObject newAmbulanceObject = GameCtrl.Instance.SV_Spawn("AAmbulance");
            AAmbulance newAmbulance = newAmbulanceObject.GetComponent<AAmbulance>();
            evacPoint.SV_AddAmbulance(newAmbulance);

            RPC_FinishCreateAmbulance(newAmbulance.netIdentity, evacPointNetIdentity);
        }

        [ClientRpc]
        private void RPC_FinishCreateAmbulance(NetworkIdentity ambulanceNetIdentity, NetworkIdentity evacPointNetIdentity)
        {
            AAmbulance newAmbulance = ambulanceNetIdentity.GetComponent<AAmbulance>();
            AEvacPoint evacPoint = evacPointNetIdentity.GetComponent<AEvacPoint>();

            newAmbulance.SetCurrentLocation(this);
            newAmbulance.SetEvacPoint(evacPoint);

            var routeCtrl = GameState.Current.m_RouteController;
            var route = routeCtrl.GetRoute(m_LocationId);
            newAmbulance.SetRoute(this, evacPoint, route.m_TravelTime, route.m_Distance);
            newAmbulance.CMD_Move(AAmbulance.EDirection.ToDestination);
        }

        [Command(requiresAuthority = false)]
        private void CMD_ReduceNumAmbulancesLeft()
        {
            m_numAmbulancesLeft--;
            if (m_numAmbulancesLeft <= 0)
                m_numAmbulancesLeft = 0;
        }
    }
}
