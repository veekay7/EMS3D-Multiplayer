using Mirror;
using UnityEngine;

namespace E3D
{
    // the action id for checking vitals by the triage officer
    public enum ECheckVitalAct { None, CanWalk, HeartRate, Respiration, BloodPressure, SpO2, CapRefillTime, GCS };

    public class E3DTriageOffrPlayer : E3DEmtPlayerBase
    {
        private float m_startTime = 0.0f;
        private TriageOffrUI m_myUI = null;
        [SerializeField, ReadOnlyVar]
        private AVictim m_victim = null;

        [SyncVar(hook = nameof(OnVictimNetIdChanged)), ReadOnlyVar]
        public uint m_victimNetId = 0;


        public AVictim CurrentVictim
        {
            get
            {
                if (isServer)
                {
                    if (m_victimNetId == 0)
                    {
                        m_victim = null;
                        return null;
                    }
                    
                    if (m_victim == null)
                        m_victim = this.GetComponentFromNetId<AVictim>(m_victimNetId);

                    return m_victim;
                }

                return m_victim;
            }
        }


        protected override void Awake()
        {
            base.Awake();
            m_useMoney = false;
        }

        protected override PlayerUIBase CreatePlayerUI()
        {
            GameObject newPlayerUIObject = Instantiate(m_PlayerUIPrefab);
            m_myUI = newPlayerUIObject.GetComponent<TriageOffrUI>();

            return m_myUI;
        }


        [Client]
        public void SetVictim(AVictim newVictim)
        {
            if (CurrentVictim != null)
            {
                CurrentVictim.Cl_Use(null);
                m_victim = null;

                CMD_SetVictim(null);
            }

            if (newVictim != null)
            {
                newVictim.Cl_Use(this);

                m_victim = newVictim;
                m_startTime = Time.time;

                CMD_SetVictim(newVictim.netIdentity);
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

        [Client]
        public void CheckVictimVital(ECheckVitalAct action)
        {
            if (CurrentVictim == null)
                return;

            CMD_CheckVictimVital(action);
        }

        [Command(requiresAuthority = false)]
        private void CMD_CheckVictimVital(ECheckVitalAct action)
        {
            switch (action)
            {
                case ECheckVitalAct.CanWalk:
                    CurrentVictim.m_State.m_CheckedCanWalk = true;
                    break;

                case ECheckVitalAct.HeartRate:
                    CurrentVictim.m_State.m_CheckedHeartRate = true;
                    break;

                case ECheckVitalAct.Respiration:
                    CurrentVictim.m_State.m_CheckedRespiration = true;
                    break;

                case ECheckVitalAct.BloodPressure:
                    CurrentVictim.m_State.m_CheckedBloodPressure = true;
                    break;

                case ECheckVitalAct.SpO2:
                    CurrentVictim.m_State.m_CheckedSpO2 = true;
                    break;

                case ECheckVitalAct.GCS:
                    CurrentVictim.m_State.m_CheckedGCS = true;
                    break;
            }

            RPC_SendResponse("check_vital", string.Empty, action.ToString());
        }

        [Client]
        public void SetPACSTag(EPACS tag)
        {
            if (CurrentVictim == null)
                return;

            CMD_SetPACSTag(tag);
        }

        [Command(requiresAuthority = false)]
        private void CMD_SetPACSTag(EPACS tag)
        {
            CurrentVictim.m_State.m_GivenPACS = tag;

            RPC_SendResponse("set_pacs", string.Empty, tag.ToString());
        }

        [Client]
        public void FinishTriage()
        {
            if (CurrentVictim == null)
                return;

            if (CurrentVictim.m_State.m_GivenPACS != EPACS.None)
            {
                GameMode gameMode = GameMode.Current;

                // if the selected victim has not been given a tag, cannot finish triage bish!!
                if (!GameMode.CanSendVictimToFirstAid(CurrentVictim))
                    return;

                bool isMarkedDeadWrongly = GameMode.CheckVictimWronglyTriagedDead(m_victim);
                if (isMarkedDeadWrongly)
                {
                    SendResponse("triage_wrong", "Wrongly triaged " + m_victim.m_GivenName + " as dead.", null);
                }
                else
                {
                    // mark the selected victim as triaged and deactivate them
                    CurrentVictim.m_State.CMD_SetTriagedFlag(true);
                    //CurrentVictim.m_IsActive = false;        // NOTE: this should not be set to false in multiplayer

                    // calculate triage time
                    float triageTime = Time.time - m_startTime;

                    // scoring is updated on the server side
                    gameMode.PlayerTriageScoring(this, CurrentVictim, triageTime);

                    // inform client about correct triage
                    SendResponse("triage_complete", CurrentVictim.m_GivenName + " has been successfully triaged.", null);

                    CMD_RemoveAndTransferVictim();

                    // reset start time to 0
                    m_startTime = 0.0f;

                    // unset the victim
                    SetVictim(null);
                }
            }
        }

        [Command(requiresAuthority = false)]
        private void CMD_RemoveAndTransferVictim()
        {
            GameMode gameMode = GameMode.Current;

            // remove the victim from the area
            CurrentLocation.SV_RemoveVictim(CurrentVictim);

            // send the victim to the first aid point
            gameMode.SendVictimToRandomFirstAidPoint(CurrentVictim);
        }

        [Client]
        public void StopTriage()
        {
            CMD_StopTriage();
            SetVictim(null);
            SendResponse("triage_cancel", string.Empty, null);
        }

        [Command(requiresAuthority = false)]
        private void CMD_StopTriage()
        {
            if (CurrentVictim != null)
            {
                CurrentVictim.m_State.m_CheckedBloodPressure = false;
                CurrentVictim.m_State.m_CheckedCanWalk = false;
                CurrentVictim.m_State.m_CheckedGCS = false;
                CurrentVictim.m_State.m_CheckedHeartRate = false;
                CurrentVictim.m_State.m_CheckedRespiration = false;
                CurrentVictim.m_State.m_CheckedSpO2 = false;
                CurrentVictim.m_State.m_GivenPACS = EPACS.None;
            }
        }

        protected override void UpdateMapView()
        {
            var faps = GameState.Current.m_FirstAidPoints.ToArray();
            var evacPoints = GameState.Current.m_EvacPoints.ToArray();
            for (int i = 0; i < faps.Length; i++)
            {
                faps[i].SetVisible(false);
            }
            for (int i = 0; i < evacPoints.Length; i++)
            {
                evacPoints[i].SetVisible(false);
            }
        }

        private void OnVictimNetIdChanged(uint oldId, uint newId)
        {
            if (isClient && !IsHumanPlayer)
            {
                if (m_victim != null && m_victim.netId == newId)
                    return;

                if (newId != 0)
                    m_victim = NetworkIdentity.spawned[newId].GetComponent<AVictim>();
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
        }
    }
}
