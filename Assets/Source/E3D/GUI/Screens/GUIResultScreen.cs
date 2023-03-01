using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace E3D
{
    public class GUIResultScreen : GUIScreen
    {
        [Header("Result Screen")]
        public GameObject m_MissionReportPanel;
        public GameObject m_PersonalReportPanel;

        [Space]

        public Toggle m_ChkboxMission;
        public Toggle m_ChkboxPersonal;

        [Space]

        public TMP_Text m_TxtMissionReport;
        public TMP_Text m_TxtPersonalReport;
        public TMP_Text m_TxtRank;


        protected override void Start()
        {
            base.Start();

            //m_ChkboxMission.isOn = false;
            //m_ChkboxPersonal.isOn = true;

            //m_MissionReportPanel.SetActive(false);
            //m_ChkboxMission.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (GameState.Current.m_CurrentMatchState == EMatchState.GameOver)
            {
                PrintMissionReport();
                PrintPersonalReport();
            }
        }

        public void SetMissionReportVisible(bool value)
        {
            m_MissionReportPanel.SetActive(value);
        }

        public void SetPersonalReportVisible(bool value)
        {
            m_PersonalReportPanel.SetActive(value);
        }

        public override void Open(UnityAction onFinishAnim = null)
        {
            // print the reports
            //PrintMissionReport();
            //PrintPersonalReport();

            if (E3DPlayer.Local != null)
            {
                if (E3DPlayer.Local is E3DTriageOffrPlayer || E3DPlayer.Local is E3DFirstAidDocPlayer || E3DPlayer.Local is E3DEvacOffrPlayer)
                {
                    m_ChkboxPersonal.gameObject.SetActive(true);
                    m_ChkboxMission.gameObject.SetActive(true);

                    m_ChkboxMission.isOn = true;
                    m_ChkboxPersonal.isOn = false;
                }
                else
                {
                    m_ChkboxPersonal.gameObject.SetActive(false);
                    m_ChkboxMission.gameObject.SetActive(true);

                    m_ChkboxMission.isOn = true;
                    m_ChkboxPersonal.isOn = false;
                }
            }

            SetMissionReportVisible(m_ChkboxMission.isOn);
            SetPersonalReportVisible(m_ChkboxPersonal.isOn);

            // NOTE: in singleplayer mode, only personal report is visible (not implemented here...)

            base.Open(onFinishAnim);
        }

        public void CloseAndReturnToMainMenu()
        {
            //if (ScreenWiper.Instance != null)
            //    ScreenWiper.Instance.SetFilled(true);

            //SceneManager.LoadScene("gamemenu");

            if (GUIController.Instance.ActiveScreen == this)
                GUIController.Instance.CloseCurrentScreen();

            if (E3DPlayer.Local != null)
                E3DPlayer.Local.Disconnect();
        }

        protected void PrintMissionReport()
        {
            //E3DPlayer player = E3DPlayer.Local;
            GameCtrl gameCtrl = GameCtrl.Instance;
            GameState gameState = GameState.Current;

            string hostname;
            string commander;
            string difficulty;
            string incidentBudget;
            string budgetUsed;
            string numPlayers;

            string numVictims;
            string numVictimsEvac;
            string numVictimsDead;
            string numVictimsTriagedCorrect;
            string numVictimsTriagedOver;
            string numVictimsTriagedUnder;

            string rank;
            string budgetStatus;

            if (gameCtrl.mode == Mirror.NetworkManagerMode.Host)
            {
                hostname = gameCtrl.m_Hostname;
                commander = gameCtrl.m_CmdrName;
                difficulty = gameCtrl.m_Difficulty.ToString();
            }
            else
            {
                var server = Globals.m_SelectedTargetServer;
                hostname = server.hostname;
                commander = server.cmdrName;
                difficulty = server.difficulty.ToString();
            }

            incidentBudget = GameMode.Current.m_MoneyGiven.ToString();
            budgetUsed = GameMode.Current.m_MoneyUsed.ToString();

            numPlayers = gameState.m_Players.Count.ToString();
            numVictims = gameState.m_Victims.Count.ToString();

            numVictimsEvac = gameState.m_NumEvacuated.ToString();
            numVictimsDead = gameState.m_NumDead.ToString();

            numVictimsTriagedCorrect = gameState.m_NumTriageCorrect.ToString();
            numVictimsTriagedOver = gameState.m_NumTriageOver.ToString();
            numVictimsTriagedUnder = gameState.m_NumTriageUnder.ToString();

            m_TxtMissionReport.text = string.Format(
                "<b>Mission name:</b> {0}\n" +
                "<b>Commander:</b> {1}\n" +
                "<b>Difficulty:</b> {2}\n" +
                "<b>Incident Budget:</b> {3}\n" +
                "<b>Budget Used:</b> {4}\n" +
                "<b>Players:</b> {5}\n" +
                "<b>Victims:</b> {6}\n" +
                "<b>Victims Evacuated:</b> {7}\n" +
                "<b>Victims Dead:</b> {8}\n" +
                "<b>Victims Correctly Triaged:</b> {9}\n" +
                "<b>Victims Over Triaged:</b> {10}\n" +
                "<b>Victims Under Triaged:</b> {11}\n\n", hostname, commander, difficulty, incidentBudget, budgetUsed, 
                numPlayers, numVictims, numVictimsEvac, numVictimsDead, 
                numVictimsTriagedCorrect, numVictimsTriagedOver, numVictimsTriagedUnder);

            int score = (int)Utils.Score_CalcICTotal(GameMode.Current.m_MoneyUsed, GameMode.Current.m_MoneyGiven);
            rank = "?";
            budgetStatus = "?";

            m_TxtRank.text = CalcICRank(score) + " " + "(" + score.ToString() + " / 100)";
        }

        protected void PrintPersonalReport()
        {
            E3DPlayer player = E3DPlayer.Local;

            if (player != null)
            {
                if (player is E3DTriageOffrPlayer)
                    PrintTriageOfficerReport(player);

                if (player is E3DFirstAidDocPlayer)
                    PrintFirstAidDocReport(player);

                if (player is E3DEvacOffrPlayer)
                    PrintEvacOfficerReport(player);
            }
        }

        private void PrintTriageOfficerReport(E3DPlayer player)
        {
            E3DTriageOffrPlayer role = (E3DTriageOffrPlayer)player;
            E3DPlayerState playerState = player.m_State;

            int totalCorrectNum = playerState.m_CorrectTriageP3Num + playerState.m_CorrectTriageP2Num + playerState.m_CorrectTriageP1Num + playerState.m_CorrectTriageP0Num;

            int totalNum = totalCorrectNum + playerState.m_UnderTriageNum + playerState.m_OverTriageNum;

            float correctPc = float.IsNaN((totalCorrectNum / (float)totalNum) * 100.0f) ? 0.0f : (totalCorrectNum / (float)totalNum) * 100.0f;

            float underTriagePc = float.IsNaN((playerState.m_UnderTriageNum / (float)totalNum) * 100.0f) ? 0.0f : (playerState.m_UnderTriageNum / (float)totalNum) * 100.0f;

            float overTriagePc = float.IsNaN((playerState.m_OverTriageNum / (float)totalNum) * 100.0f) ? 0.0f : (playerState.m_OverTriageNum / (float)totalNum) * 100.0f;

            float avgTime = float.IsNaN(playerState.m_TotalTriageTime / totalNum) ? 0.0f : playerState.m_TotalTriageTime / totalNum;

            float maxActionScore = float.IsNaN(totalNum * Consts.SCORE_TRIAGE_CORRECT) ? 0.0f : totalNum * Consts.SCORE_TRIAGE_CORRECT;

            float maxDmgScore = float.IsNaN(totalNum * Consts.SCORE_TRIAGE_DAMAGE_RANGE0) ? 0.0f : totalNum * Consts.SCORE_TRIAGE_DAMAGE_RANGE0;

            float actionScore = float.IsNaN(playerState.m_TriageActionScore / maxActionScore) ? 0.0f : playerState.m_TriageActionScore / maxActionScore;

            float dmgScore = float.IsNaN(playerState.m_TriageDmgScore / maxDmgScore) ? 0.0f : playerState.m_TriageDmgScore / maxDmgScore;

            string reportString = string.Format("<b>Subject:</b> {0}\n" +
                "<b>Role:</b> Triage Officer\n\n" +
                "<b>Correct Triage (%): </b> {1}\n" +
                "<indent=15%>P3: {2}</indent>\n" +
                "<indent=15%>P2: {3}</indent>\n" +
                "<indent=15%>P1: {4}</indent>\n" +
                "<indent=15%>Dead: {5}</indent>\n\n" +
                "<b>Under Triage (%):</b> {6}\n\n" +
                "<b>Over Triage (%):</b> {7}\n\n" +
                "<b>Average Decision Time (secs):</b> {8}\n",
                playerState.Player.m_PlayerName,
                (int)correctPc, playerState.m_CorrectTriageP3Num, playerState.m_CorrectTriageP2Num, playerState.m_CorrectTriageP1Num, playerState.m_CorrectTriageP0Num,
                (int)underTriagePc, (int)overTriagePc, avgTime);

            m_TxtPersonalReport.text = reportString;
            //m_TxtRank.text = playerState.m_TriageDmgScore.ToString();
            //m_TxtRank.text = Utils.Score_CalcTriageOfficerTotal(playerState.m_TriageActionScore / maxActionScore, playerState.m_TriageDmgScore / maxDmgScore).ToString();

            int score = (int)Utils.Score_CalcTriageOfficerTotal(actionScore, dmgScore);

            m_TxtRank.text = CalcRank(score) + " " + "(" + score.ToString() + " / 100)";
        }

        private void PrintFirstAidDocReport(E3DPlayer player)
        {
            E3DFirstAidDocPlayer role = (E3DFirstAidDocPlayer)player;
            E3DPlayerState playerState = player.m_State;

            int totalNum = playerState.m_TotalVictimsAttendedNum;

            float maxDmgScore = float.IsNaN(totalNum * 10) ? 0.0f : totalNum * 10;      // TODO: put this as constant???

            float actionScore = float.IsNaN(playerState.m_TreatmentActionScore / (float)totalNum) ? 0.0f : playerState.m_TreatmentActionScore / (float)totalNum;

            float avgTime = float.IsNaN(playerState.m_TotalTreatmentTime / (float)totalNum) ? 0.0f : playerState.m_TotalTreatmentTime / (float)totalNum;

            float dmgScore = float.IsNaN(playerState.m_TreatmentDmgScore / maxDmgScore) ? 0.0f : playerState.m_TreatmentDmgScore / maxDmgScore;

            float accuracy = float.IsNaN(actionScore * 100.0f) ? 0.0f : actionScore * 100.0f;

            string reportString = string.Format("<b>Subject:</b> {0}\n" +
                "<b>Role:</b> First Aid Point Officer\n\n" +
                "<b>Total victims attended to:</b> {1}\n" +
                "<b>Average treatment time per victim (secs):</b> {2}\n" +
                "<b>Treatment accuracy:</b> {3} %\n", //+
                //"<b>Victims dead under treatment:</b> {4}\n",
                player.m_PlayerName, totalNum, avgTime, accuracy.ToString("0.00"), "?");

            //Debug.Log("Action Score: " + actionScore + ", Dmg Score: " + playerState.m_TreatmentDmgScore);

            m_TxtPersonalReport.text = reportString;

            int score = (int)Utils.Score_CalcFAPDocTotal(actionScore, dmgScore);
            m_TxtRank.text = CalcRank(score) + " " + "(" + score.ToString() + " / 100)";
        }

        private void PrintEvacOfficerReport(E3DPlayer player)
        {
            E3DEvacOffrPlayer role = (E3DEvacOffrPlayer)player;
            E3DPlayerState playerState = player.m_State;

            int totalNum = playerState.m_TotalVictimsEvacuatedNum;

            float maxDmgScore = float.IsNaN(totalNum * 10) ? 0.0f : totalNum * 10;      // NOTE: put this as constant???

            float actionScore = float.IsNaN(playerState.m_EvacActionScore / (float)totalNum) ? 0.0f : playerState.m_EvacActionScore / (float)totalNum;

            //float avgTime = playerState.m_TotalTreatmentTime / (float)totalNum;

            float dmgScore = float.IsNaN(playerState.m_EvacDmgScore / maxDmgScore) ? 0.0f : playerState.m_EvacDmgScore / maxDmgScore;

            float accuracy = float.IsNaN(actionScore * 100.0f) ? 0.0f : actionScore * 100.0f;

            string reportString = string.Format("<b>Subject: {0}\n" +
               "Role: Evacuation Officer</b>\n\n" +
                "<b>Evacuated victims:</b> {1}\n",
               player.m_PlayerName, totalNum);

            m_TxtPersonalReport.text = reportString;

            int score = (int)Utils.Score_CalcEvacOfficerTotal(actionScore, dmgScore);

            m_TxtRank.text = CalcRank(score) + " " + "(" + score.ToString() + " / 100)";
        }
        public string CalcICRank(int score)
        {
            if (score <= 70)
            {
                return "A+";
            }
            else if (score <= 80)
            {
                return "A";
            }
            else if (score <= 90)
            {
                return "A-";
            }
            else if (score <= 100)
            {
                return "B+";
            }
            else if (score <= 110)
            {
                return "B";
            }
            else if (score <= 120)
            {
                return "B-";
            }
            else if (score <= 140)
            {
                return "C";
            }
            else if (score <= 160)
            {
                return "D";
            }
            else if (score <= 180)
            {
                return "E";
            }
            else
            {
                return "F";
            }
        }
        public string CalcRank(int score)
        {
            if (score >= 95)
            {
                return "A+";
            }
            else if (score >= 90)
            {
                return "A";
            }
            else if (score >= 85)
            {
                return "A-";
            }
            else if (score >= 80)
            {
                return "B+";
            }
            else if (score >= 75)
            {
                return "B";
            }
            else if (score >= 70)
            {
                return "B-";
            }
            else if (score >= 65)
            {
                return "C";
            }
            else if (score >= 60)
            {
                return "D";
            }
            else if (score >= 55)
            {
                return "E";
            }
            else
            {
                return "F";
            }
        }
    }
}
