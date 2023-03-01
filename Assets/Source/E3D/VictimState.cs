using Mirror;

namespace E3D
{
    public class VictimState : NetworkBehaviour
    {
        [SyncVar, ReadOnlyVar]
        public bool m_IsTriaged;
        [SyncVar, ReadOnlyVar]
        public bool m_IsTreated;
        [SyncVar, ReadOnlyVar]
        public bool m_IsEvacuated;
        [SyncVar, ReadOnlyVar]
        public bool m_CheckedCanWalk;
        [SyncVar, ReadOnlyVar]
        public bool m_CheckedHeartRate;
        [SyncVar, ReadOnlyVar]
        public bool m_CheckedRespiration;
        [SyncVar, ReadOnlyVar]
        public bool m_CheckedBloodPressure;
        [SyncVar, ReadOnlyVar]
        public bool m_CheckedSpO2;
        [SyncVar, ReadOnlyVar]
        public bool m_CheckedGCS;
        [SyncVar, ReadOnlyVar]
        public bool m_IsLoadedIntoVehicle;
        [SyncVar, ReadOnlyVar]
        public EPACS m_GivenPACS;
        [SyncVar, ReadOnlyVar]
        public float m_AmbulanceTime;   // i know it makes no sense, but shaddap about it


        private void Reset()
        {
            Clear();
        }

        public void Clear()
        {
            m_IsTriaged = false;
            m_IsTreated = false;
            m_IsEvacuated = false;
            m_CheckedCanWalk = false;
            m_CheckedHeartRate = false;
            m_CheckedRespiration = false;
            m_CheckedBloodPressure = false;
            m_CheckedSpO2 = false;
            m_CheckedGCS = false;
            m_GivenPACS = EPACS.None;
            m_AmbulanceTime = 0.0f;
        }

        [Server]
        public void SetAllVitalsCheckedFlag(bool value)
        {
            m_CheckedCanWalk = value;
            m_CheckedHeartRate = value;
            m_CheckedRespiration = value;
            m_CheckedBloodPressure = value;
            m_CheckedSpO2 = value;
            m_CheckedGCS = value;
        }

        [Command(requiresAuthority = false)]
        public void CMD_SetAllVitalsCheckedFlag(bool value)
        {
            SetAllVitalsCheckedFlag(value);
        }

        [Command(requiresAuthority = false)]
        public void CMD_SetTriagedFlag(bool value) { m_IsTriaged = value; }

        [Command(requiresAuthority = false)]
        public void CMD_SetTreatedFlag(bool value) { m_IsTreated = value; }

        [Command(requiresAuthority = false)]
        public void CMD_SetEvacuatedFlag(bool value) { m_IsEvacuated = value; }

        [Command(requiresAuthority = false)]
        public void CMD_SetAmbulantFlag(bool value) { m_CheckedCanWalk = value; }

        [Command(requiresAuthority = false)]
        public void CMD_SetHeartRateFlag(bool value) { m_CheckedHeartRate = value; }

        [Command(requiresAuthority = false)]
        public void CMD_SetRespirationFlag(bool value) { m_CheckedRespiration = value; }

        [Command(requiresAuthority = false)]
        public void CmdSetBloodPressureFlag(bool value) { m_CheckedBloodPressure = value; }

        [Command(requiresAuthority = false)]
        public void CmdSetSpO2Flag(bool value) { m_CheckedSpO2 = value; }

        [Command(requiresAuthority = false)]
        public void CmdSetCRTFlag(bool value) { m_CheckedGCS = value; }

        [Command(requiresAuthority = false)]
        public void CmdSetGivenPACS(EPACS pac) { m_GivenPACS = pac; }

        public bool Equals(VictimState other)
        {
            bool isEqual = this.m_IsTriaged == other.m_IsTriaged &&
                this.m_IsTreated == other.m_IsTreated &&
                this.m_IsEvacuated == other.m_IsEvacuated &&
                this.m_CheckedCanWalk == other.m_CheckedCanWalk &&
                this.m_CheckedHeartRate == other.m_CheckedHeartRate &&
                this.m_CheckedRespiration == other.m_CheckedRespiration &&
                this.m_CheckedBloodPressure == other.m_CheckedBloodPressure &&
                this.m_CheckedSpO2 == other.m_CheckedSpO2 &&
                this.m_CheckedGCS == other.m_CheckedGCS &&
                this.m_GivenPACS == other.m_GivenPACS;
            return isEqual;
        }
    }
}
