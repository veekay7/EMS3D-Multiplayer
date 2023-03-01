using UnityEngine;

namespace E3D
{
    public class E3DTriageOffrCpu : E3DTriageOffrPlayer
    {
        public override bool IsHumanPlayer { get => false; }

        private AVictim currentVictim;
        private float nextThink;
        private float thinkDuration = 2;
        private EPACS currentTag;
        private int errorRate = 0;


        protected override void Cl_Think()
        {
            if (m_curLocation != null && Time.time > nextThink)
            {
                nextThink = Time.time + thinkDuration;
                if (m_curLocation.GetVictims().Length > 0)
                {
                    currentVictim = m_curLocation.GetVictims()[0];
                    if (currentVictim.IsPlayerUsing)
                        return;

                    this.SetVictim(currentVictim);
                    currentVictim.m_State.CMD_SetAllVitalsCheckedFlag(true);

                    if (!currentVictim.m_State.m_IsTriaged)
                    {
                        currentVictim.m_State.CMD_SetTriagedFlag(true);
                        if (!currentVictim.IsAlive)
                        {
                            currentTag = EPACS.P0;
                        }
                        else
                        {
                            if (currentVictim.m_CanWalk)
                            {
                                currentTag = EPACS.P3;
                            }
                            else
                            {
                                if (currentVictim.m_Respiration >= 10.0f && currentVictim.m_Respiration <= 30.0f)
                                {
                                    if (currentVictim.m_HeartRate >= 70.0f && currentVictim.m_HeartRate <= 120.0f)
                                    {
                                        currentTag = EPACS.P2;
                                    }
                                    else
                                    {
                                        currentTag = EPACS.P1;
                                    }
                                }
                                else
                                {
                                    currentTag = EPACS.P1;
                                }
                            }
                        }

                        currentTag = RandomizePAC(errorRate, currentTag);
                        this.SetPACSTag(currentTag);
                        this.FinishTriage();
                    }
                }
            }
        }

        private EPACS RandomizePAC(int prob, EPACS tag)
        {
            int number = ((int)tag + UnityEngine.Random.Range(0, 1)) % 3 + 1;

            if (UnityEngine.Random.Range(1, 100) <= prob)
            {
                if (tag == EPACS.P0)
                {
                    return tag;
                }
                else
                {
                    if (number == 1)
                    {
                        return EPACS.P1;
                    }
                    else if (number == 2)
                    {
                        return EPACS.P2;
                    }
                    else
                    {
                        return EPACS.P1;
                    }
                }
            }
            else
            {
                return tag;
            }
        }

        public override void SV_AssumeControl(GameObject oldPlayerObject)
        {
            if (oldPlayerObject == null)
                return;

            base.SV_AssumeControl(oldPlayerObject);

            E3DTriageOffrPlayer oldPlayer = oldPlayerObject.GetComponent<E3DTriageOffrPlayer>();
            m_victimNetId = oldPlayer.m_victimNetId;
        }
    }
}
