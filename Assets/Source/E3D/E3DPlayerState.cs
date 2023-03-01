using Mirror;
using System;
using System.Collections;
using UnityEngine;

namespace E3D
{
    public class E3DPlayerState : NetworkBehaviour
    {
        [SerializeField, ReadOnlyVar]
        private E3DPlayer m_player;
        [SyncVar, ReadOnlyVar]
        private uint m_playerNetId;

        [Header("Triage Officer State")]
        [SyncVar, ReadOnlyVar]
        public float m_TriageActionScore;
        [SyncVar, ReadOnlyVar]
        public float m_TriageDmgScore;
        [SyncVar, ReadOnlyVar]
        public int m_CorrectTriageP3Num;
        [SyncVar, ReadOnlyVar]
        public int m_CorrectTriageP2Num;
        [SyncVar, ReadOnlyVar]
        public int m_CorrectTriageP1Num;
        [SyncVar, ReadOnlyVar]
        public int m_CorrectTriageP0Num;
        [SyncVar, ReadOnlyVar]
        public int m_UnderTriageNum;
        [SyncVar, ReadOnlyVar]
        public int m_OverTriageNum;
        [SyncVar, ReadOnlyVar]
        public float m_TotalTriageTime;

        [Header("First Aid Point Officer State")]
        //public SyncList<E3D_VictimTreatmentState> m_TreatmentStates = new SyncList<E3D_VictimTreatmentState>();
        [SyncVar, ReadOnlyVar]
        public bool m_WasDeadWhileInCare;
        [SyncVar, ReadOnlyVar]
        public int m_TotalVictimsAttendedNum;
        [SyncVar, ReadOnlyVar]
        public int m_TotalTreatmentNum;
        [SyncVar, ReadOnlyVar]
        public int m_CorrectTreatmentNum;
        [SyncVar, ReadOnlyVar]
        public float m_TreatmentActionScore;
        [SyncVar, ReadOnlyVar]
        public float m_TreatmentDmgScore;
        [SyncVar, ReadOnlyVar]
        public float m_TotalTreatmentTime;

        [Header("Evacuation Officer State")]
        [SyncVar, ReadOnlyVar]
        public int m_TotalVictimsEvacuatedNum;
        [SyncVar, ReadOnlyVar]
        public float m_EvacActionScore;
        [SyncVar, ReadOnlyVar]
        public float m_EvacDmgScore;


        public E3DPlayer Player
        {
            get
            {
                if (isServer)
                {
                    if (m_playerNetId == 0)
                        return null;
                    var p = NetworkIdentity.spawned[m_playerNetId].GetComponent<E3DPlayer>();
                    return p;
                }

                return m_player;
            }
        }

        public int TotalTriagedNum
        {
            get
            {
                return m_CorrectTriageP3Num + m_CorrectTriageP2Num + 
                    m_CorrectTriageP1Num + m_CorrectTriageP0Num +
                    m_UnderTriageNum + m_OverTriageNum;
            }
        }


        private void Awake()
        {
            // triage officer state
            //m_AttendedVictimNum = 0;
            m_CorrectTriageP3Num = 0;
            m_CorrectTriageP2Num = 0;
            m_CorrectTriageP1Num = 0;
            m_CorrectTriageP0Num = 0;
            m_UnderTriageNum = 0;
            m_OverTriageNum = 0;

            // first aid point doc state
            m_WasDeadWhileInCare = false;
            m_TotalVictimsAttendedNum = 0;
            m_TotalTreatmentNum = 0;
            m_CorrectTreatmentNum = 0;
            m_TreatmentActionScore = 0;
            m_TreatmentDmgScore = 0;
            m_TotalTreatmentTime = 0;

            // evac officer state
            m_TotalVictimsEvacuatedNum = 0;
            m_EvacActionScore = 0;
            m_EvacDmgScore = 0;
        }

        public void SetPlayer(E3DPlayer player)
        {
            m_player = player;
            if (m_player != null)
                CMD_SetPlayer(m_player.netIdentity);
            else
                CMD_SetPlayer(null);
        }

        [Command(requiresAuthority = false)]
        private void CMD_SetPlayer(NetworkIdentity playerNetIdentity)
        {
            if (playerNetIdentity != null)
                m_playerNetId = playerNetIdentity.netId;
            else
                m_playerNetId = 0;
        }

        [Command(requiresAuthority = false)]
        public void CMD_UpdateTriageScore(uint victimNetId, float triageTime)
        {
            int actionScore = 0;

            AVictim victim = NetworkIdentity.spawned[victimNetId].GetComponent<AVictim>();
            EPACS currentVictimPAC = GameMode.CheckVictimPAC(victim);

            if (currentVictimPAC == victim.m_State.m_GivenPACS)
            {
                switch (victim.m_State.m_GivenPACS)
                {
                    case EPACS.P3:
                        m_CorrectTriageP3Num++;
                        break;

                    case EPACS.P2:
                        m_CorrectTriageP2Num++;
                        break;

                    case EPACS.P1:
                        m_CorrectTriageP1Num++;
                        break;

                    case EPACS.P0:
                        m_CorrectTriageP0Num++;
                        break;
                }

                actionScore = Consts.SCORE_TRIAGE_CORRECT;
            }
            else
            {
                int pac_comp = GameMode.ComparePACS(victim.m_State.m_GivenPACS, currentVictimPAC, out int difference);
                if (pac_comp == 1)
                {
                    // over triaged
                    m_OverTriageNum++;

                    // check how much the was over triaged
                    if (difference == 1)
                        actionScore = Consts.SCORE_TRIAGE_OVER1;
                    else if (difference == 2)
                        actionScore = Consts.SCORE_TRIAGE_OVER2;
                }
                else if (pac_comp == -1)
                {
                    // under triaged
                    m_UnderTriageNum++;

                    if (difference == -1)
                        actionScore = Consts.SCORE_TRIAGE_UNDER1;
                    else if (difference == -2)
                        actionScore = Consts.SCORE_TRIAGE_UNDER2;
                }
            }

            // accumulate time
            m_TotalTriageTime += triageTime;

            float dmgScore = (GameMode.CalcDamageScore(victim.m_StartHealth, victim.m_CurHealth));

            m_TriageActionScore += actionScore;
            m_TriageDmgScore += dmgScore;
        }

        [Command(requiresAuthority = false)]
        public void CMD_TallyTreatmentTallies(int treatNumAmount, int correctTreatAmount)
        {
            m_TotalTreatmentNum += treatNumAmount;
            m_CorrectTreatmentNum += correctTreatAmount;
        }

        [Command(requiresAuthority = false)]
        public void CMD_UpdateTreatmentScore(uint victimNetId, float treatmentDuration)
        {
            AVictim victim = NetworkIdentity.spawned[victimNetId].GetComponent<AVictim>();

            // tally
            m_TotalVictimsAttendedNum++;

            // this one checks to see if the victim has a "no action" tag and has not yet applied any treatment to,
            // they will get a score of m_TreatmentCount++ and m_CorrectTreament++.
            if (victim.HasInjury && victim.ContainsTreatmentTag("no action"))
            {
                m_TotalTreatmentNum++;
                m_CorrectTreatmentNum++;
            }

            float actionScore = m_CorrectTreatmentNum / (float)m_TotalTreatmentNum;
            float dmgScore = GameMode.CalcDamageScore(victim.m_StartHealth, victim.m_CurHealth);

            Debug.Log("Dmg Score after treatment per victim: " + dmgScore);

            m_TreatmentActionScore += actionScore;
            m_TreatmentDmgScore += dmgScore;

            //float timeScore = GetTreatmentTimeScore(dmgScore, victim.m_State.m_GivenPACS);
            //float totalTimeScore = m_VictimManager.NumVictims * Consts.SCORE_MAX_TIME;

            // accumulate time
            m_TotalTreatmentTime += treatmentDuration;
        }

        [Command(requiresAuthority = false)]
        public void CMD_UpdateMorgueScore(uint victimNetId, float treatmentDuration)
        {
            AVictim victim = NetworkIdentity.spawned[victimNetId].GetComponent<AVictim>();

            // tally
            m_TotalTreatmentNum++;
            m_TotalVictimsAttendedNum++;

            if (victim.m_StartHealth <= float.Epsilon)
            {
                m_CorrectTreatmentNum++;
            }

            float actionScore = m_CorrectTreatmentNum / (float)m_TotalTreatmentNum;
            float dmgScore;

            if (victim.m_StartHealth <= float.Epsilon)
                dmgScore = 10.0f;
            else
                dmgScore = GameMode.CalcDamageScore(victim.m_StartHealth, victim.m_CurHealth);

            m_TreatmentActionScore += actionScore;
            m_TreatmentDmgScore += dmgScore;

            // accumulate time
            m_TotalTreatmentTime += treatmentDuration;
        }

        [Command(requiresAuthority = false)]
        public void CMD_UpdateEvacScore(uint playerNetIdNum, uint ambulanceNetIdNum)
        {
            E3DPlayer player = NetworkIdentity.spawned[playerNetIdNum].GetComponent<E3DPlayer>();
            AAmbulance ambulance = NetworkIdentity.spawned[ambulanceNetIdNum].GetComponent<AAmbulance>();
            AVictim victimInAmbulance = ambulance.Victim;

            m_TotalVictimsEvacuatedNum++;

            // get the player role
            E3DEvacOffrPlayer evacOffr = (E3DEvacOffrPlayer)player;

            var victimList = evacOffr.CurrentLocation.GetVictims();
            int victimListLen = victimList.Length;

            Array.Resize(ref victimList, victimListLen + 1);

            victimListLen = victimList.Length;
            victimList[victimListLen - 1] = victimInAmbulance;

            int[] currentState = { 0, 0, 0, 0, 0, 0 }; //p3,p3,p2,p2js,p1,p1js
            string[] allType = { "p3", "p3js", "p2", "p2js", "p1", "p1js" };

            ArrayList currentArrayType = new ArrayList();

            for (int i = 0; i < victimListLen; i++)
            {
                var victim = victimList[i];
                if (victim.m_State.m_GivenPACS == EPACS.P3)
                {
                    if ((victim.m_Age < 16 || victim.m_IsPregnant))
                        currentState[1] = 1;
                    else
                        currentState[0] = 1;
                }
                else if (victim.m_State.m_GivenPACS == EPACS.P2)
                {
                    if ((victim.m_Age < 16 || victim.m_IsPregnant))
                        currentState[3] = 1;
                    else
                        currentState[2] = 1;
                }
                else if (victim.m_State.m_GivenPACS == EPACS.P1)
                {
                    if ((victim.m_Age < 16 || victim.m_IsPregnant))
                        currentState[4] = 1;
                    else
                        currentState[4] = 1;
                }
                else
                {
                    currentState[5] = 1;
                }
            }

            for (int i = 0; i < currentState.Length; i++)
            {
                if (currentState[i] == 1)
                {
                    currentArrayType.Add(allType[i]);
                }
            }

            // check victim's V value
            string victim_v_score = GameMode.GetVictimV(victimInAmbulance);

            float evacActionScore = GameMode.CalcEvacActionScore1(victim_v_score, currentArrayType);

            Debug.Log("Evac Act Score: " + evacActionScore);
            m_EvacActionScore += evacActionScore;

            float delay = GameMode.CalcTrafficDelay(DateTime.Now);
            float travelTime = ambulance.TravelTime * (1 + delay);

            victimInAmbulance.m_State.m_AmbulanceTime = travelTime - victimInAmbulance.m_CurStableTime;

            float dmgScore = GameMode.CalcDamageScore(victimInAmbulance.m_StartHealth, victimInAmbulance.m_CurHealth);
            m_EvacDmgScore += dmgScore;

        }

        [ClientRpc]
        private void RPC_DebugLog(string msg)
        {
            Debug.Log(msg);
        }
    }


    // TODO: consider putting some of the vars in this in the victim state for tracking
    // the same variables can then be added to the player state
    [Serializable]
    public class E3D_VictimTreatmentState
    {
        public uint m_VictimNetId;              // the victim that was being treated
        public bool m_WasDeadWhileInCare;       // did the victim die while being in care
        public int m_TotalTreatmentNum;         // total treatments applied
        public int m_CorrectTreatmentNum;       // total correct treatments applied
        public int m_BadTreatmentNum;           // total useless treatments applied
        public int m_FailedTreatmentCount;      // total failed treatments applied
        public float m_TotalTreatmentTime;      // total treatment time


        public bool IsPerfect()
        {
            return (m_FailedTreatmentCount != 0) && (m_BadTreatmentNum != 0);
        }
    }
}
