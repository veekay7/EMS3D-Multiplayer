using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using URand = UnityEngine.Random;

namespace E3D
{
    // the current state of the match
    public enum EMatchState { Enter = 0, WaitingToStart = 1, InProgress = 2, GameOver = 3 }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(VictimManager))]
    [RequireComponent(typeof(GameState))]
    public class GameMode : NetworkBehaviour
    {
        [ReadOnlyVar]
        public GameState m_State;
        [ReadOnlyVar]
        public VictimManager m_VictimManager;

        [Header("Debug")]
        public bool m_DbgTriageAllVictims = false;
        public bool m_DbgTreatAllVictims = false;

        // how much money is allocated for this mission
        [SyncVar]
        public int m_MoneyGiven;

        // the current amout of money used for this mission
        // gibe the kush biyatch!!
        [SyncVar]
        public int m_MoneyUsed;

        [SyncVar, SerializeField, ReadOnlyVar]
        private EMatchState m_matchState;
        [SyncVar]
        private float m_elapsedGameTime;


        public static GameMode Current { get; private set; }

        public bool AllVictimsTriaged { get => m_State.m_NumTriaged == m_VictimManager.NumVictims; }

        public bool AllVictimsTreated { get => m_State.m_NumTreated == m_VictimManager.NumVictims; }

        public bool AllVictimsEvacuated { get => m_State.m_NumEvacuated == m_VictimManager.NumVictims; }

        public bool AllVictimsDead { get => m_State.m_NumDead == m_VictimManager.NumVictims; }

        public float ElapsedGameTime { get => m_elapsedGameTime; }


        private void Awake()
        {
            if (Current != null && Current == this)
                return;

            m_State = GetComponent<GameState>();
            m_VictimManager = GetComponent<VictimManager>();

            Current = this;
        }

        public override void OnStartServer()
        {
            m_matchState = EMatchState.Enter;
            m_elapsedGameTime = 0.0f;

            // set the money bitches, based on difficulty selected
            m_MoneyGiven = Globals.m_CurrentMap.m_BudgetAlloc[(int)GameCtrl.Instance.m_Difficulty];
            if (m_MoneyGiven <= 0)
                m_MoneyGiven = 1;   // given money cannot be less than or equal to 0
            m_MoneyUsed = 0;
        }

        private void Update()
        {
            if (!isServer)
                return;

            /* game has just entered the map */
            if (m_matchState == EMatchState.Enter)
            {
                var gameSys = GameCtrl.Instance;

                int maxVictimCount = gameSys.m_VictimSettings.m_NumVictims;
                int probp3 = gameSys.m_VictimSettings.m_Probability_P3;
                int probp2 = gameSys.m_VictimSettings.m_Probability_P2;
                int probp1 = gameSys.m_VictimSettings.m_Probability_P1;
                int probp0 = gameSys.m_VictimSettings.m_Probability_P0;
                int agegap = gameSys.m_VictimSettings.m_AgeGap;

                if (!m_VictimManager.FinishedCreatingVictims)
                {
                    m_VictimManager.CreateVictims(maxVictimCount);
                }
                else
                {
                    // when all victims have spawned (on the server) initialise all
                    // spawned victims with injuries and other profile things
                    m_VictimManager.InitInfoToVictims(probp3, probp2, probp1, probp0, agegap);

                    // do any debug shit here
                    if (m_DbgTriageAllVictims)
                    {
                        m_VictimManager.TriageAllCorrectly();
                        m_State.m_NumTriaged = m_VictimManager.NumVictims;
                        m_State.m_NumTriageCorrect = m_VictimManager.NumVictims;
                    }

                    if (m_DbgTreatAllVictims)
                    {
                        m_VictimManager.TreatAll();
                        m_State.m_NumTreated = m_VictimManager.NumVictims;
                        for (int i = 0; i < m_VictimManager.NumVictims; i++)
                        {
                            AVictim victim = m_VictimManager.GetVictims()[i];
                            if (!victim.IsAlive)
                                m_State.m_NumDead++;
                        }
                    }

                    // now wait to start
                    SetMatchState(EMatchState.WaitingToStart);
                }
            }

            /* game waiting to start */
            if (m_matchState == EMatchState.WaitingToStart)
            {
                return;
            }

            /* game is in progress */
            if (m_matchState == EMatchState.InProgress)
            {
                m_elapsedGameTime += Time.deltaTime;
                Update_InProgress();
            }

            /* game has ended */
            if (m_matchState == EMatchState.GameOver)
            {
                return;
            }
        }

        [Command(requiresAuthority = false)]
        public void CMD_StartGame(NetworkConnectionToClient sender = null)
        {
            if (m_matchState == EMatchState.WaitingToStart)
            {
                if (E3DPlayer.Local != null)
                {
                    OnBeginMatch();
                    SetMatchState(EMatchState.InProgress);
                }
            }
        }

        [Server]
        public void CalcMoneyGiven(EDifficulty difficulty)
        {
            m_MoneyGiven = 0;

            int numTriageOff = 1;
            int numFAPOff = 1;
            int numEvacOff = 1;

            int num_p1 = numOfP1(m_VictimManager.m_victims);
            int num_p2 = numOfP2(m_VictimManager.m_victims);
            int num_p3 = numOfP3(m_VictimManager.m_victims);

            float timeToTriage = 2.0f;
            float timeToTreat = 3.0f;
            float timeToEvacuate = 3.0f;

            float timeP1 = 3.0f;
            float timeP2 = 6.0f;
            float timeP3 = 9.0f;

            int d;

            int ambulanceCost = 1500;
            int triageCost = 3000;
            int fapCost = 3500;
            int evacCost = 2000;

            if (difficulty == EDifficulty.Easy)
            {
                d = 1;
                m_MoneyGiven = 300000;
            }
            else if (difficulty == EDifficulty.Medium)
            {
                d = 2;
                m_MoneyGiven = 200000;
            }
            else if (difficulty == EDifficulty.Hard)
            {
                d = 3;
                m_MoneyGiven = 150000;
            }
            else
            {
                d = 1;
                m_MoneyGiven = 300000;
            }

            if ((timeToTriage * (float)num_p1) / (timeP1 * (float)d * (float)numTriageOff) > 1)
            {
                numTriageOff = (int)Math.Ceiling((timeToTriage * (float)num_p1) / (timeP1 * (float)d));
            }
            if ((timeToTriage * (float)num_p2) / (timeP2 * (float)d * (float)numTriageOff) > 1)
            {
                numTriageOff = (int)Math.Ceiling((timeToTriage * (float)num_p2) / (timeP2 * (float)d));
            }
            if ((timeToTriage * (float)num_p3) / (timeP3 * (float)d * (float)numTriageOff) > 1)
            {
                numTriageOff = (int)Math.Ceiling((timeToTriage * (float)num_p3) / (timeP3 * (float)d));
            }

            if ((timeToTreat * (float)num_p1) / (timeP1 * (float)d * (float)numFAPOff) > 1)
            {
                numFAPOff = (int)Math.Ceiling((timeToTreat * (float)num_p1) / (timeP1 * (float)d));
            }
            if ((timeToTreat * (float)num_p2) / (timeP2 * (float)d * (float)numFAPOff) > 1)
            {
                numFAPOff = (int)Math.Ceiling((timeToTreat * (float)num_p2) / (timeP2 * (float)d));
            }
            if ((timeToTreat * (float)num_p3) / (timeP3 * (float)d * (float)numFAPOff) > 1)
            {
                numFAPOff = (int)Math.Ceiling((timeToTreat * (float)num_p3) / (timeP3 * (float)d));
            }

            if ((timeToEvacuate * (float)num_p1) / (timeP1 * (float)d * (float)numEvacOff) > 1)
            {
                numEvacOff = (int)Math.Ceiling((timeToEvacuate * (float)num_p1) / (timeP1 * (float)d));
            }
            if ((timeToEvacuate * (float)num_p2) / (timeP2 * (float)d * (float)numEvacOff) > 1)
            {
                numEvacOff = (int)Math.Ceiling((timeToEvacuate * (float)num_p2) / (timeP2 * (float)d));
            }
            if ((timeToEvacuate * (float)num_p3) / (timeP3 * (float)d * (float)numEvacOff) > 1)
            {
                numEvacOff = (int)Math.Ceiling((timeToEvacuate * (float)num_p3) / (timeP3 * (float)d));
            }

            m_MoneyGiven += numTriageOff * triageCost + numFAPOff * fapCost + numEvacOff * evacCost;




        }

        public int numOfP1(List<AVictim> victimList)
        {
            int count = 0;
            for (int i = 0; i < victimList.Count; i++)
            {
                if (victimList[i].m_PACS == EPACS.P1)
                    count++;
            }
            return count;
        }

        public int numOfP2(List<AVictim> victimList)
        {
            int count = 0;
            for (int i = 0; i < victimList.Count; i++)
            {
                if (victimList[i].m_PACS == EPACS.P2)
                    count++;
            }
            return count;
        }

        public int numOfP3(List<AVictim> victimList)
        {
            int count = 0;
            for (int i = 0; i < victimList.Count; i++)
            {
                if (victimList[i].m_PACS == EPACS.P3)
                    count++;
            }
            return count;
        }

        [Command(requiresAuthority = false)]
        public void CMD_UseMoney(int amount)
        {
            UseMoney(amount);
        }

        [Server]
        public void UseMoney(int amount)
        {
            if (amount < 0)
                Debug.LogWarning("CMD_AddUsedMoney: amount cannot be negative. But it's okay, I will convert it to positive value for you, and also becase I have a Jaaaaag.");
            m_MoneyUsed += Mathf.Abs(amount);
        }

        public bool IsOverBudget()
        {
            return (m_MoneyUsed > m_MoneyGiven);
        }

        public void SetMatchState(EMatchState newState)
        {
            if (isServer)
            {
                if (m_matchState == newState)
                    return;

                EMatchState lastState = m_matchState;
                m_matchState = newState;

                m_State.m_LastMatchState = lastState;
                m_State.m_CurrentMatchState = newState;
            }
        }

        protected bool ArePlayersReady()
        {
            int numPlayers = GameCtrl.Instance.numPlayers;
            var players = m_State.m_Players;

            if (numPlayers >= 2 && players.Count == numPlayers)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    // if even one player is not ready, then the game is not ready to start
                    if (!players[i].IsReady)
                        return false;
                }

                return true;
            }

            return false;
        }

        [ClientRpc]
        private void RPC_SetEmtToLocation(NetworkIdentity emtNetId, NetworkIdentity locNetId)
        {
            E3DEmtPlayerBase emt = emtNetId.GetComponent<E3DEmtPlayerBase>();
            ALocationPoint location = locNetId.GetComponent<ALocationPoint>();

            if (emt is E3DFirstAidDocPlayer && location is AFirstAidPoint)
            {
                E3DFirstAidDocPlayer firstAidDoc = (E3DFirstAidDocPlayer)emt;
                AFirstAidPoint fap = (AFirstAidPoint)location;
                firstAidDoc.SetLocation(fap);

                //fap.AddPlayer(firstAidDoc);
            }
            else if (emt is E3DEvacOffrPlayer && location is AEvacPoint)
            {
                E3DEvacOffrPlayer evacOffr = (E3DEvacOffrPlayer)emt;
                AEvacPoint evacPoint = (AEvacPoint)location;
                evacOffr.SetLocation(evacPoint);

                //evacPoint.AddPlayer(evacOffr);
            }
        }

        [Server]
        protected void OnBeginMatch()
        {
            var victims = m_VictimManager.GetVictims();
            AFirstAidPoint[] faps = m_State.m_FirstAidPoints.ToArray();
            AEvacPoint[] evacPoints = m_State.m_EvacPoints.ToArray();
            AAmbulance[] ambulances = m_State.m_Ambulances.ToArray();

            // distribute victims
            for (int i = 0; i < victims.Count; i++)
            {
                var v = victims[i];

                // handle some of the debug functionality first
                if (m_DbgTreatAllVictims)
                {
                    SendVictimToRandomEvacPoint(v);
                    continue;
                }
                else if (m_DbgTriageAllVictims)
                {
                    SendVictimToRandomFirstAidPoint(v);
                    continue;
                }

                SendVictimToRandomCasualtyPoint(v);
            }

            // distribute ambulances
            for (int i = 0; i < ambulances.Length; i++)
            {
                int idx = URand.Range(0, evacPoints.Length);
                AEvacPoint curEvacPoint = evacPoints[idx];

                curEvacPoint.SV_AddAmbulance(ambulances[i]);
                ambulances[i].SV_SetEvacPoint(curEvacPoint, true);
            }

            // distribute players to post
            E3DPlayer[] players = m_State.m_Players.ToArray();

            // randomly place players to their respective posts depending on their selected role
            for (int i = 0; i < players.Length; i++)
            {
                E3DPlayer p = players[i];
                if (p is E3DFirstAidDocPlayer)
                {
                    // send officer to a first aid point
                    int idx = URand.Range(0, faps.Length);
                    AFirstAidPoint fap = faps[idx];

                    RPC_SetEmtToLocation(p.netIdentity, fap.netIdentity);
                }
                else if (p is E3DEvacOffrPlayer)
                {
                    // send officer to an evac point
                    int idx = URand.Range(0, faps.Length);
                    AEvacPoint evacPoint = evacPoints[idx];

                    RPC_SetEmtToLocation(p.netIdentity, evacPoint.netIdentity);
                }

                // enable input for all players
                p.RPC_EnableInput(true);
            }

            m_VictimManager.StartUpdate();
        }

        [Server]
        protected void Update_InProgress()
        {
            if ((AllVictimsTriaged && AllVictimsTreated && AllVictimsEvacuated) || AllVictimsDead)
            {
                Debug.Log("Game Over");

                // game over!!
                OnEndMatch();
                SetMatchState(EMatchState.GameOver);
            }
        }

        [Server]
        protected void OnEndMatch()
        {
            // inform all players that the game is over
            if (E3DPlayer.Local == null)
                return;

            for (int i = 0; i < m_State.m_Players.Count; i++)
            {
                E3DPlayer player = m_State.m_Players[i];
                player.RPC_GameOver();
            }
        }

        [Server]
        public void SendVictimToRandomCasualtyPoint(AVictim victim)
        {
            var areas = m_State.m_CasualtyPoints.ToArray();
            if (areas != null && areas.Length > 0)
            {
                int idx = URand.Range(0, areas.Length);
                areas[idx].SV_AddVictim(victim);
            }
        }

        [Server]
        public void SendVictimToRandomFirstAidPoint(AVictim victim)
        {
            var firstAidPoints = m_State.m_FirstAidPoints.ToArray();
            if (firstAidPoints != null && firstAidPoints.Length > 0)
            {
                int idx = URand.Range(0, firstAidPoints.Length);
                firstAidPoints[idx].SV_AddVictim(victim);
            }
        }

        [Server]
        public void SendVictimToRandomEvacPoint(AVictim victim)
        {
            var evacPoints = m_State.m_EvacPoints.ToArray();
            if (evacPoints != null && evacPoints.Length > 0)
            {
                int idx = URand.Range(0, evacPoints.Length);
                evacPoints[idx].SV_AddVictim(victim);
            }
        }

        public static bool CheckVictimWronglyTriagedDead(AVictim victim)
        {
            // End the game immediately if victim is wrongly triaged as dead and vice versa!!
            if ((victim.m_PACS != EPACS.P0 && victim.m_State.m_GivenPACS == EPACS.P0) ||
                (victim.m_PACS == EPACS.P0 && victim.m_State.m_GivenPACS != EPACS.P0))
                return true;

            return false;
        }

        public static bool CanSendVictimToFirstAid(AVictim victim)
        {
            return (victim.m_State.m_GivenPACS != EPACS.None) || (victim.m_State.m_GivenPACS != EPACS.P0);
        }

        public static EPACS CheckVictimPAC(AVictim victim)
        {
            if (victim.m_Respiration == 0 || victim.m_HeartRate == 0)
            {
                return EPACS.P0;
            }
            else if (victim.m_CanWalk)
            {
                return EPACS.P3;
            }
            else if (victim.m_Respiration >= 10 && victim.m_Respiration <= 30)
            {
                if (victim.m_HeartRate >= 70 && victim.m_HeartRate <= 120)
                {
                    return EPACS.P2;
                }
                else
                {
                    return EPACS.P1;
                }
            }
            else
            {
                return EPACS.P1;
            }
        }

        /// <summary>
        /// Compares two PACs scale enums. The target for comparison is always the first parameter.
        /// Example: if assigned is P3 and correct is P2, then the result is -1.
        /// </summary>
        /// <param name="assigned"></param>
        /// <param name="correct"></param>
        /// <returns>
        /// Returns 0 if equal, -1 if under triaged, 1 if over triaged, 
        /// 666 if triaged alive wrongly as dead, -666 if triaged dead wrongly as alive.
        /// difference outputs the difference between assigned and correct PACS.
        /// If equal, <out>difference</out> is 0. If under or over triaged, <out>difference</out> will be 1, 2, -1, or -2.
        /// If triaged alive wrongly as dead <out>difference</out> is 666 and -666 if triaged dead wrongly as alive.
        /// </returns>
        public static int ComparePACS(EPACS assigned, EPACS correct, out int difference)
        {
            int iAssigned = (int)assigned;
            int iCorrect = (int)correct;

            difference = iAssigned - iCorrect;

            if (iAssigned > iCorrect)
            {
                return 1;
            }
            else if (iAssigned < iCorrect)
            {
                return -1;
            }
            else if (iAssigned == 0 && iCorrect != 0)
            {
                difference = 666;
                return 666;
            }
            else if (iAssigned != 0 && iCorrect == 0)
            {
                difference = -666;
                return -666;
            }

            return 0;
        }

        /// <summary>
        /// Finds the difference in PAC tags.
        /// </summary>
        /// <param name="assigned"></param>
        /// <param name="correct"></param>
        /// <returns>difference value</returns>
        public static int PACSDifference(EPACS assigned, EPACS correct)
        {
            int iAssigned = (int)assigned;
            int iCorrect = (int)correct;
            int difference = iAssigned - iCorrect;

            return difference;
        }


        #region Scoring Shit

        public static int CalcDamageScore(float initialHealth, float currentHealth)
        {
            if (Mathf.Abs(initialHealth) <= Mathf.Epsilon)
                return 0;

            float fDamagePc = ((initialHealth - currentHealth) / initialHealth) * 100.0f;
            int damagePc = (int)fDamagePc;

            if (damagePc >= 0 && damagePc <= 10)
                return 10;
            if (damagePc >= 11 && damagePc <= 20)
                return 9;
            if (damagePc >= 21 && damagePc <= 30)
                return 8;
            if (damagePc >= 31 && damagePc <= 40)
                return 7;
            if (damagePc >= 41 && damagePc <= 50)
                return 6;
            if (damagePc >= 51 && damagePc <= 60)
                return 5;
            if (damagePc >= 61 && damagePc <= 70)
                return 4;
            if (damagePc >= 71 && damagePc <= 80)
                return 3;
            if (damagePc >= 81 && damagePc <= 90)
                return 2;
            if (damagePc >= 91 && damagePc <= 98)
                return 1;

            return 0;
        }

        public static int GetTreatmentTimeScore(float treatmentTime, EPACS pacs)
        {
            int avgTime;

            switch (pacs)
            {
                case EPACS.P3:
                    avgTime = Consts.TREATMENT_AVGTIME_P3;
                    break;

                case EPACS.P2:
                    avgTime = Consts.TREATMENT_AVGTIME_P3;
                    break;

                case EPACS.P1:
                    avgTime = Consts.TREATMENT_AVGTIME_P3;
                    break;

                default:
                    avgTime = 0;
                    break;
            }

            if (avgTime == 0)
                return 0;

            int time = (int)((treatmentTime / avgTime) * 100.0f);

            if (time >= 100)
                return 5;
            else if (time >= 101 && time <= 125)
                return 4;
            else if (time >= 126 && time <= 150)
                return 3;
            else if (time >= 151 && time <= 175)
                return 2;
            else if (time >= 176 && time <= 200)
                return 1;

            return 0;
        }

        /// <summary>
        /// Scoring for triage officer
        /// </summary>
        /// <param name="player"></param>
        /// <param name="victim"></param>
        /// <param name="triageDuration"></param>
        [Client]
        public void PlayerTriageScoring(E3DPlayer player, AVictim victim, float triageDuration)
        {
            if (player == null)
            {
                Debug.Log("cannot process triage score, player param is null.");
                return;
            }

            if (victim == null)
            {
                Debug.Log("cannot process triage score, victim param is null.");
                return;
            }

            // update player triage scoring
            player.m_State.CMD_UpdateTriageScore(victim.netId, triageDuration);

            // update game triage scoring
            m_State.CMD_UpdateTriageScore(victim.netId);
        }

        [Client]
        public void ProcessTreatmentScoring(E3DPlayer player, AVictim victim, float treatmentDuration)
        {
            if (player == null)
            {
                Debug.Log("cannot process treatment score, player param is null.");
                return;
            }

            if (victim == null)
            {
                Debug.Log("cannot process treatment score, victim param is null.");
                return;
            }

            // update player treatment scoring
            player.m_State.CMD_UpdateTreatmentScore(victim.netId, treatmentDuration);

            // update game treatment scoring
            m_State.CMD_UpdateTreatmentScore();
        }

        [Client]
        public void ProcessMorgueScoring(E3DPlayer player, AVictim victim, float treatmentDuration)
        {
            if (player == null)
            {
                Debug.Log("cannot process morgue score, player param is null.");
                return;
            }

            if (victim == null)
            {
                Debug.Log("cannot process morgue score, victim param is null.");
                return;
            }

            // update player morgue scoring
            player.m_State.CMD_UpdateMorgueScore(victim.netId, treatmentDuration);

            // update game morgue scoring
            m_State.CMD_UpdateMorgueScore();
        }

        // edited by joshua
        [Client]
        public void ProcessEvacScoring(E3DPlayer player, AAmbulance ambulance)
        {
            // update player evac score
            player.m_State.CMD_UpdateEvacScore(player.netId, ambulance.netId);

            // NOTE: Moved to AAmbulance RPC_OnArriveAtDestination()
            // update the game evac score
            //m_State.CMD_UpdateEvacScore();
        }

        /// <summary>
        /// The V value represents the possible scores of all victim PAC when calculating actual score for evac.
        /// Don't ask why it's V its not vagina bro!! Don't think you can get some!
        /// </summary>
        /// <param name="victim"></param>
        /// <returns></returns>
        /// edited by joshua
        public static string GetVictimV(AVictim victim)
        {
            if (victim.m_State.m_GivenPACS == EPACS.P3)
            {
                if ((victim.m_Age < 16 || victim.m_IsPregnant))
                    return "p3js";
                else
                    return "p3";
            }
            else if (victim.m_State.m_GivenPACS == EPACS.P2)
            {
                if ((victim.m_Age < 16 || victim.m_IsPregnant))
                    return "p2js";
                else
                    return "p2";
            }
            else if (victim.m_State.m_GivenPACS == EPACS.P1)
            {
                if ((victim.m_Age < 16 || victim.m_IsPregnant))
                    return "p1js";
                else
                    return "p1";
            }

            return "p0";
        }

        // edited by Joshua (entire calctrafficdelay function)
        public static float CalcTrafficDelay(DateTime now)
        {
            float currentCongestionLevel;

            // based on real data of singapore traffic on 2019 (monday to friday)
            float[] congestionLevel = {
            2, 0, 0, 0, 0, 1, 20, 48, 59, 43, 30, 27, 26, 28, 30, 30, 31, 42, 58, 41, 24, 20, 15, 7,
            2, 0, 0, 0, 0, 2, 21, 48, 58, 43, 32, 29, 28, 30, 32, 31, 31, 42, 59, 42, 25, 22, 17, 9,
            3, 0, 0, 0, 0, 2, 20, 47, 59, 45, 32, 29, 28, 29, 32, 30, 30, 43, 60, 43, 26, 22, 17, 8,
            2, 0, 0, 0, 0, 2, 20, 45, 55, 43, 33, 30, 29, 30, 33, 32, 33, 45, 62, 45, 27, 24, 19, 9,
            3, 0, 0, 0, 0, 2, 19, 44, 52, 43, 34, 32, 32, 31, 37, 35, 36, 52, 69, 49, 29, 28, 26, 15,
            7, 2, 0, 0, 0, 1, 10, 14, 21, 28, 33, 37, 37, 37, 35, 34, 33, 33, 36, 33, 25, 25, 23, 14,
            6, 1, 0, 0, 0, 0, 6, 8, 13, 16, 20, 23, 24, 24, 22, 21, 21, 22, 22, 21, 20, 18, 13, 7 };

            // use system time
            int day;

            switch (now.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    day = 0;
                    break;
                case DayOfWeek.Tuesday:
                    day = 1;
                    break;
                case DayOfWeek.Wednesday:
                    day = 2;
                    break;
                case DayOfWeek.Thursday:
                    day = 3;
                    break;
                case DayOfWeek.Friday:
                    day = 4;
                    break;
                case DayOfWeek.Saturday:
                    day = 5;
                    break;
                case DayOfWeek.Sunday:
                    day = 6;
                    break;
                default:
                    day = 0;
                    break;
            }

            int hour = now.Hour;
            int minute = now.Minute;
            int n = day * 24 + hour;

            currentCongestionLevel = ((minute / 60.0f) * (congestionLevel[(n + 1) % 168] - congestionLevel[n % 168]) + congestionLevel[n % 168]) / 100.0f;

            return currentCongestionLevel;
        }

        // edited by Joshua
        public static float CalcEvacActionScore1(string v, ArrayList s)
        {
            int score = s.IndexOf(v) + 1;

            return (float)score / (float)s.Count;
        }

        #endregion


        // editor only
        private void OnValidate()
        {
            m_VictimManager = gameObject.GetOrAddComponent<VictimManager>();
            m_State = gameObject.GetOrAddComponent<GameState>();
        }
    }


    /// <summary>
    /// Instance wrapper for E3D_GameMode and its derived classes
    /// The implicit operators means that you do not need to explicitly set or get the Value property of MyProp, 
    /// but can write code to access the value in a more "natural" way.
    /// Reference: https://stackoverflow.com/questions/2587236/generic-property-in-c-sharp
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GameModeInstance<T> where T : GameMode
    {
        private T m_instance;

        public T Instance
        {
            get => m_instance;
            set => m_instance = value;
        }

        public static implicit operator T(GameModeInstance<T> value)
        {
            return value.Instance;
        }

        public static implicit operator GameModeInstance<T>(T value)
        {
            return new GameModeInstance<T> { Instance = value };
        }
    }
}
