using Mirror;
using System;
using UnityEngine;

namespace E3D
{
    // patient sex (if you dare change this i'll fucking kill you!
    public enum ESex { Female = 0, Male = 1 }

    // capiliary refill time
    public enum ECapRefillTimeType { NoResponse = 0, LessThan2 = 1, MoreThan2 = 2 }

    /// <summary>
    /// Victim actor
    /// </summary>
    [RequireComponent(typeof(VictimState))]
    public class AVictim : NetworkBehaviour
    {
        [HideInInspector]
        public VictimState m_State;

        public const float m_Resitance = 1.075f;
        public const float m_Elasticity = 0.597f;
        public const float m_StrokeVolume = 72.0f;

        public VictimPortraitList m_PortraitList;
        public Sprite m_PortraitSprite;

        [SyncVar]
        public string m_GivenName = "unnamed";
        [SyncVar]
        public bool m_IsActive = true;
        [SyncVar]
        public bool m_CanBePregnant = false;
        [SyncVar]
        public bool m_IsPregnant = false;
        [SyncVar]
        public ESex m_Sex = ESex.Male;
        [SyncVar]
        public EPACS m_PACS = EPACS.P3;

        [SyncVar]
        public int m_Age = 0;
        [SyncVar]
        public int m_AgeLo = 0;
        [SyncVar]
        public int m_AgeHi = 0;
        [SyncVar(hook = nameof(OnPortraitIndexChanged))]
        public int m_PortraitIndex = -1;

        [SyncVar]
        public float m_CurHealth = 0.0f;
        [SyncVar]
        public float m_HealthSinceGameStart = 0.0f;
        [SyncVar]
        public float m_StartHealth = 0.0f;

        [SyncVar]
        public bool m_CanWalk = true;

        [SyncVar]
        public float m_StartHeartRate = 0.0f;
        [SyncVar]
        public int m_HeartRate = 0;

        [SyncVar]
        public float m_StartResp = 0.0f;
        [SyncVar]
        public int m_Respiration = 0;

        [SyncVar]
        public BloodPressure m_BloodPressure = BloodPressure.Zero;

        [SyncVar]
        public float m_StartSpO2 = 0.0f;
        [SyncVar]
        public float m_SpO2 = 0.0f;

        [SyncVar]
        public float m_StartGCS = 0.0f;
        [SyncVar]
        public float m_GCS = 0.0f;
        //[SyncVar]
        //public ECapRefillTimeType     m_CapRefillTimeType;
        [SyncVar]
        public int m_InjuryIdx = 0;
        [SyncVar]
        public string m_TreatmentTags = null;
        [SyncVar]
        public float m_CurStableTime = 0.0f;

        private float m_s_Resp = 0.0f;
        private float m_s_HR = 0.0f;
        private float m_s_GCS = 0.0f;
        private float m_s_Total = 0.0f;
        private float m_stableTime = 0.0f;
        [SerializeField, ReadOnlyVar]
        private E3DPlayer m_player = null;

        [SyncVar]
        private bool m_initialised = false;
        [SyncVar(hook = nameof(OnPlayerNetIdChanged))]
        private uint m_playerNetId = 0;


        public bool IsAlive { get => m_CurHealth > 0; }

        public bool IsPlayerUsing { get => m_player != null; }

        public E3DPlayer UsingPlayer
        {
            get
            {
                return m_player;
            }
        }

        public int InjuryIdx { get => m_InjuryIdx; }

        public string TreatmentTags { get => m_TreatmentTags; }

        public bool HasInjury { get => (m_InjuryIdx == -1 && string.IsNullOrEmpty(m_TreatmentTags)); }


        protected void Awake()
        {
            m_State = GetComponent<VictimState>();
        }

        public override void OnStartClient()
        {
            //// if this victim object is created when a client joins the game, we must set the portrait here
            //if (m_initialised)
            //{
            //    // set the portrait (must be set on client side and replicated to all clients)
            //    m_PortraitSprite = m_PortraitList.GetPortraitByIndex(m_Sex, m_Age, m_PortraitIndex);
            //}

            if (GameState.Current != null)
            {
                GameState.Current.AddVictim(this);
            }
        }

        [Server]
        public void SV_Init(int id, ESex sex, int age, int age_lo, int age_hi, EPACS scale)
        {
            if (m_initialised)
                return;

            m_Sex = sex;
            m_Age = age;
            m_AgeLo = age_lo;
            m_AgeHi = age_hi;
            m_PACS = scale;

            // give the victim a name
            m_GivenName = "Victim #" + id.ToString();
            
            // setthe portrait index
            // get the portrait index on the server and use the hook on the portraitIndex value to change on client side!!
            int portraitIndex = -1;
            if (sex == ESex.Female)
                m_PortraitList.GetFemalePortraitByAgeRange(age, out portraitIndex);
            else
                m_PortraitList.GetMalePortraitByAgeRange(age, out portraitIndex);
            m_PortraitIndex = portraitIndex;

            // apply the initial vitals
            if (m_PACS == EPACS.P3)
            {
                SetVitalsByPAC(EPACS.P3);
            }
            else if (m_PACS == EPACS.P2)
            {
                SetVitalsByPAC(EPACS.P2);
            }
            else if (m_PACS == EPACS.P1)
            {
                SetVitalsByPAC(EPACS.P1);
            }
            else if (m_PACS == EPACS.P0)
            {
                SetVitalsByPAC(EPACS.P0);
            }
            else
            {
                SetVitalsByPAC(EPACS.None);
            }

            // set initial vitals here
            m_StartSpO2 = m_SpO2;
            m_StartResp = m_Respiration;
            m_StartGCS = m_GCS;
            m_StartHeartRate = m_HeartRate;

            // do vital scoring shit
            if (m_StartResp <= 20)
            {
                m_s_Resp = 5.15529f / (1 + Mathf.Exp(-m_StartResp / 3.74014f + 1.39551f)) - 1.01516f;
            }
            else
            {
                m_s_Resp = 4.29751f / (1 + Mathf.Exp(m_StartResp / 5.68283f - 6.17549f));
            }

            m_s_HR = Mathf.Pow(200 - m_StartHeartRate, 5.0f) * Mathf.Pow(14.0f + m_StartHeartRate, 5.0f) * 2.0335f * Mathf.Pow(10.0f, -20.0f);
            m_s_GCS = 2.43555f * Mathf.Log(m_StartGCS) - 2.63742f;
            m_s_Total = m_s_Resp + m_s_HR + m_s_GCS;

            // set initial health
            m_HealthSinceGameStart = CalcNewHealth(0.0f);   // because we start at 0 yo!
            m_CurHealth = m_HealthSinceGameStart;
            m_stableTime = CalcStableTime(0.0f, 0.0f, 0.0f);

            m_initialised = true;
        }

        [ClientRpc]
        private void RPC_SetPortraitOld(ESex sex, int age)
        {
            int index = -1;
            if (sex == ESex.Female)
            {
                m_PortraitSprite = m_PortraitList.m_NullFemaleSprite;
                m_PortraitSprite = m_PortraitList.GetFemalePortraitByAgeRange(age, out m_PortraitIndex);
            }
            else
            {
                m_PortraitSprite = m_PortraitList.m_NullMaleSprite;
                m_PortraitSprite = m_PortraitList.GetMalePortraitByAgeRange(age, out m_PortraitIndex);
            }

            CMD_SetPortaritIndex(index);
        }

        [Command(requiresAuthority = false)]
        private void CMD_SetPortaritIndex(int portraitIdx)
        {
            m_PortraitIndex = portraitIdx;
        }

        private void OnPortraitIndexChanged(int oldIndex, int newIndex)
        {
            // set the portrait (must be set on client side and replicated to all clients)
            m_PortraitSprite = m_PortraitList.GetPortraitByIndex(m_Sex, m_Age, newIndex);
        }

        [Client]
        public void Cl_Use(E3DPlayer player)
        {
            if (player != null)
                CMD_Use(player.netIdentity);
            else
                CMD_Use(null);
        }

        [Command(requiresAuthority = false)]
        public void CMD_Use(NetworkIdentity playerIdentity)
        {
            if (playerIdentity != null)
            {
                E3DPlayer player = playerIdentity.GetComponent<E3DPlayer>();
                if (player == null)
                {
                    Debug.LogError("Cannot use this victim, playerIdentity is not a E3DPlayer type.");
                    return;
                }

                m_playerNetId = playerIdentity.netId;
            }
            else
            {
                m_playerNetId = 0;
            }
        }

        private void OnPlayerNetIdChanged(uint oldId, uint newId)
        {
            if (oldId != newId)
            {
                if (newId == 0)
                {
                    m_player = null;
                    return;
                }

                m_player = this.GetComponentFromNetId<E3DPlayer>(newId);

                //if (NetworkServer.active)
                //{
                //    NetworkIdentity newPlayerIdentity = NetworkServer.spawned[newId];
                //    m_player = newPlayerIdentity.GetComponent<E3DPlayer>();
                //}

                //if (NetworkClient.active)
                //{
                //    NetworkIdentity newPlayerIdentity = NetworkClient.spawned[newId];
                //    m_player = newPlayerIdentity.GetComponent<E3DPlayer>();
                //}
            }        
        }

        public void AddHealth(float amount)
        {
            m_CurHealth += amount;
            if (m_CurHealth > Consts.MAX_VICTIM_HEALTH)
                m_CurHealth = Consts.MAX_VICTIM_HEALTH;
        }

        public void RemoveHealth(float amount)
        {
            m_CurHealth -= amount;
            if (m_CurHealth <= 0)
            {
                m_CurHealth = 0.0f;
                m_PACS = EPACS.P0;
                SetVitalsByPAC(EPACS.P0);

                //var victimManager = E3D_VictimManager.Current;
                //if (victimManager != null)
                //    victimManager.m_DeadCount += 1;
            }
        }

        [Server]
        public float CalcNewHealth(float time)
        {
            if (m_s_Total <= float.Epsilon)
                return 0.0f;

            float result = (m_s_Total * 100.0f / 12.0f) / (Mathf.Pow(((time / 30) /
                Mathf.Pow(2.11876f, (0.50176f * m_s_Total - 2.26823f))), (2.46462f / (1 + Mathf.Exp(0.58548f * m_s_Total - 7.24525f)))) + 1);
            return result;
        }

        [Server]
        public void UpdateHealthAndVitals(float time)
        {
            m_CurHealth = CalcNewHealth(time);
            float deltaHealth = m_HealthSinceGameStart - m_CurHealth;

            /* adjust vitals */
            m_HeartRate = (int)AdjustHeartRate(deltaHealth * m_s_HR / m_s_Total / 25.0f);
            m_Respiration = (int)AdjustRespiration(deltaHealth * m_s_Resp / m_s_Total / 25.0f);
            m_BloodPressure = new BloodPressure(Mathf.RoundToInt(((3 * m_Resitance * m_HeartRate * m_StrokeVolume / 60) + (2 * m_StrokeVolume * m_Elasticity)) / 3), Mathf.RoundToInt(((3 * m_Resitance * m_HeartRate * m_StrokeVolume / 60) - (2 * m_StrokeVolume * m_Elasticity)) / 3));
            m_SpO2 = Mathf.RoundToInt(UnityEngine.Random.Range(m_StartSpO2-3, m_SpO2+3));
            if(m_SpO2 >= 100)
            {
                m_SpO2 = 100;
            }
            else if(m_SpO2 <=0)
            {
                m_SpO2 = 0;
            }
            m_GCS = AdjustGCS(deltaHealth * m_s_GCS / m_s_Total / 25);

            if (m_CurHealth <= Consts.VERY_SMOL_HEALTH)
            {
                m_CurHealth = 0;
                m_PACS = EPACS.P0;
                SetVitalsByPAC(m_PACS);
            }
        }

        [Server]
        public void SetVitalsByPAC(EPACS scale)
        {
            switch (scale)
            {
                case EPACS.P3:
                    SetVitals(true, UnityEngine.Random.Range(80,110), UnityEngine.Random.Range(15,25), 99, UnityEngine.Random.Range(13,15));
                    break;

                case EPACS.P2:
                    SetVitals(false, UnityEngine.Random.Range(70, 120), UnityEngine.Random.Range(10, 30), 95, UnityEngine.Random.Range(9,12));
                    break;

                case EPACS.P1:
                    int i = UnityEngine.Random.Range(1, 2);
                    if(i == 1)
                    {
                        if(UnityEngine.Random.Range(1, 2)== 1)
                        {
                            SetVitals(false, UnityEngine.Random.Range(30, 160), UnityEngine.Random.Range(4,9), 90, UnityEngine.Random.Range(3, 8));
                        }
                        else
                        {
                            SetVitals(false, UnityEngine.Random.Range(30, 160), UnityEngine.Random.Range(31,60), 90, UnityEngine.Random.Range(3, 8));
                        }
                    }else if (i == 2)
                    {
                        if (UnityEngine.Random.Range(1, 2) == 1)
                        {
                            SetVitals(false, UnityEngine.Random.Range(30, 69), UnityEngine.Random.Range(4, 60), 90, UnityEngine.Random.Range(3, 8));
                        }
                        else
                        {
                            SetVitals(false, UnityEngine.Random.Range(121, 160), UnityEngine.Random.Range(4, 60), 90, UnityEngine.Random.Range(3, 8));
                        }
                    }
                    break;

                case EPACS.P0:
                    SetVitals(false, 0, 0, 0, 0.0f);
                    break;

                case EPACS.None:
                    SetVitals(true, 60, 14, 100, 15.0f);
                    break;
            }
        }

        [Server]
        public void SetVitals(bool canWalk, int heartRate, int respiration, float spO2, float gcs)
        {
            m_CanWalk = canWalk;
            m_HeartRate = heartRate;
            m_Respiration = respiration;
            m_SpO2 = spO2;
            m_GCS = gcs;
            //m_CapRefillTimeType = capRefillTimeType;
        }

        [Server]
        public void SetInjury(Injury injury)
        {
            m_InjuryIdx = InjuryList.IndexOf(injury);
            m_TreatmentTags = injury.m_TreatmentTags;

            //RemoveHealth(injury.m_Damage);
        }

        public string FindTreatmentTag(string tag)
        {
            if (m_TreatmentTags.Contains(tag))
            {
                string[] tags = m_TreatmentTags.Split(',');
                for (int i = 0; i < tags.Length; i++)
                {
                    if (tags[i].Equals(tag))
                        return tags[i];
                }
            }

            return null;
        }

        public bool ContainsTreatmentTag(string tag)
        {
            return m_TreatmentTags.Contains(tag);
        }

        [Command(requiresAuthority = false)]
        public void CMD_RemoveTreatmentTag(string tag)
        {
            string[] tags = m_TreatmentTags.Split(',');
            m_TreatmentTags = string.Empty;

            // reconstruct the treatment tags string
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i].Equals(tag))
                    continue;

                if (i < tags.Length - 1)
                    m_TreatmentTags = string.Concat(m_TreatmentTags, tags[i], ",");
                else
                    m_TreatmentTags = string.Concat(m_TreatmentTags, tags[i]);
            }
        }

        [Command(requiresAuthority = false)]
        public void CMD_ClearAllTreatmentTags()
        {
            m_TreatmentTags = null;
        }

        public bool RequiresTreatment()
        {
            return !string.IsNullOrEmpty(m_TreatmentTags);
        }

        // JOSHUA: 
        public float CalcStableTime(float hr, float resp, float gcs)
        {
            return 10.0f;
        }

        private float AdjustHeartRate(float scaledHealth)
        {
            int result = 0;
            float a = 200.0f;
            float b = 14.0f;
            float c = 5.0f;
            float d = 2.0335f * Mathf.Pow(10, -20);
            float s_initHeartRate = Mathf.Pow(a - m_StartHeartRate, c) * Mathf.Pow(b + m_StartHeartRate, c) * d;
            float HR1 = (float)(((a - b) / 2.0f) - 0.5 * Mathf.Pow(d, (-1.0f / (2.0f * c))) * Mathf.Sqrt(a * a * Mathf.Pow(d, 1.0f / c) + 2.0f * a * b * Mathf.Pow(d, 1.0f / c) + b * b * Mathf.Pow(d, 1.0f / c) - 4.0f * Mathf.Pow(s_initHeartRate - scaledHealth, 1.0f / c)));
            float HR2 = (float)(((a - b) / 2.0f) + 0.5 * Mathf.Pow(d, (-1.0f / (2.0f * c))) * Mathf.Sqrt(a * a * Mathf.Pow(d, 1.0f / c) + 2.0f * a * b * Mathf.Pow(d, 1.0f / c) + b * b * Mathf.Pow(d, 1.0f / c) - 4.0f * Mathf.Pow(s_initHeartRate - scaledHealth, 1.0f / c)));

            if ((m_StartHeartRate - HR1) > (m_StartHeartRate - HR2))
            {
                result = Mathf.RoundToInt(HR2);
            }
            else
            {
                result = Mathf.RoundToInt(HR1);
            }

            if (result > 0 && result <= 200)
            {
                return result;
            }

            return 0;
        }

        private float AdjustRespiration(float scaledHealth)
        {
            float s_initRR;
            int result;
            if (m_StartResp <= 20)
            {
                s_initRR = 5.15529f / (1 + Mathf.Exp(-m_StartResp / 3.74014f + 1.39551f)) - 1.01516f;
                result = Mathf.RoundToInt((float)(-3.74014f * (Math.Log(5.15529f / ((s_initRR - scaledHealth) + 1.01516f) - 1) + 1.39551)));
            }
            else
            {
                s_initRR = 4.29751f / (1 + Mathf.Exp(m_StartResp / 5.68283f - 6.17549f));
                result = Mathf.RoundToInt((float)(5.68283f * (Mathf.Log(4.29751f / (s_initRR - scaledHealth) - 1) + 6.17549)));
            }

            if (result > 0 && result <= 60)
            {
                return result;
            }

            return 0;
        }

        private float AdjustGCS(float scaledHealth)
        {
            float s_initGCS = 2.43555f * Mathf.Log(m_StartGCS) - 2.63742f;
            float result = Mathf.RoundToInt(Mathf.Exp(((s_initGCS - scaledHealth) + 2.63742f) / 2.43555f));
            if (result <= 15 && result >= 3)
            {
                return result;
            }

            return 0;
        }

        public override void OnStopClient()
        {
            if (GameState.Current != null)
            {
                GameState.Current.RemoveVictim(this);
            }
        }

        private void OnValidate()
        {
            if (m_State == null)
            {
                m_State = GetComponent<VictimState>();
                if (m_State == null)
                    m_State = gameObject.AddComponent<VictimState>();
            }
        }
    }


    /// <summary>
    /// Blood pressure
    /// </summary>
    [Serializable]
    public struct BloodPressure : IEquatable<BloodPressure>
    {
        public static BloodPressure Zero = new BloodPressure(0, 0);

        public int systolic;        // systolic blood pressure
        public int diastolic;       // diastolic blood pressure


        public BloodPressure(int systolic, int diastolic)
        {
            this.systolic = systolic;
            this.diastolic = diastolic;
        }

        public override string ToString()
        {
            return systolic.ToString() + "/" + diastolic.ToString();
        }

        public bool Equals(BloodPressure other)
        {
            return this.systolic == other.systolic && this.diastolic == other.diastolic;
        }
    }


    /// <summary>
    /// Injury Slot
    /// </summary>
    [Serializable]
    public class InjurySlot
    {
        public int InjuryIdx;                             // the index for the injury in the injury list
        public string TreatmentTags;                      // the list of treatment tags required to cure this injury. separated by '|' character


        public InjurySlot()
        {
            InjuryIdx = -1;
            TreatmentTags = string.Empty;
        }

        public InjurySlot(int injuryIdx, string treatmentTags)
        {
            InjuryIdx = injuryIdx;
            TreatmentTags = treatmentTags;
        }

        public string FindTreatmentTag(string tag)
        {
            if (TreatmentTags.Contains(tag))
            {
                string[] tags = TreatmentTags.Split(',');
                for (int i = 0; i < tags.Length; i++)
                {
                    if (tags[i].Equals(tag))
                        return tags[i];
                }
            }

            return null;
        }

        public bool RemoveTreatmentTag(string tag)
        {
            if (TreatmentTags.Contains(tag))
            {
                string[] tags = TreatmentTags.Split(',');
                TreatmentTags = string.Empty;

                // reconstruct the treatment tags string
                for (int i = 0; i < tags.Length; i++)
                {
                    if (tags[i].Equals(tag))
                        continue;
                    if (i < tags.Length - 1)
                        TreatmentTags = string.Concat(TreatmentTags, tags[i], ",");
                    else
                        TreatmentTags = string.Concat(TreatmentTags, tags[i]);
                }

                return true;
            }

            return false;
        }

        [Command(requiresAuthority = false)]
        private void CMD_UpdateTreatmentTags()
        {

        }

        public bool ContainsTag(string tag)
        {
            return TreatmentTags.Contains(tag);
        }

        public bool HasTags()
        {
            return !string.IsNullOrEmpty(TreatmentTags);
        }

        public bool Equals(InjurySlot other)
        {
            return InjuryIdx == other.InjuryIdx && TreatmentTags.Equals(other.TreatmentTags);
        }
    }
}
