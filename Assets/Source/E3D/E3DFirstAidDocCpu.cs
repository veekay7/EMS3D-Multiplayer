using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace E3D
{
    public class E3DFirstAidDocCpu : E3DFirstAidDocPlayer
    {
        public override bool IsHumanPlayer { get => false; }

        private AVictim currentVictim;
        private float nextThink;
        private float thinkDuration = 2;
        private int errorRate = 0;
        private AVictim[] sortedVictim;
        private string[] treatmentList;
        private bool m_isThinking = false;


        private IEnumerator Co_Think()
        {
            m_isThinking = true;

            sortedVictim = sortVictimList(m_curLocation.GetVictims());

            if (UnityEngine.Random.Range(1, 100) <= errorRate)
            {
                currentVictim = sortedVictim[UnityEngine.Random.Range(0, sortedVictim.Length)];
            }
            else
            {
                currentVictim = sortedVictim[0];
            }

            if (currentVictim.IsPlayerUsing)
            {
                currentVictim = null;
                m_isThinking = false;
                nextThink = Time.time + thinkDuration;
                yield break;
            }

            this.SetVictim(currentVictim);

            if (!currentVictim.m_State.m_IsTreated)
            {
                currentVictim.m_State.CMD_SetTreatedFlag(true);

                while (!currentVictim.m_State.m_IsTreated)
                    yield return null;

                if (!currentVictim.IsAlive)
                {
                    this.SendVictimToMorgue();
                }
                else
                {
                    //treatmentList = currentVictim.m_TreatmentTags.Split(',');
                    //this.CurrentVictim.m_TreatmentTags = "";
                    currentVictim.CMD_ClearAllTreatmentTags();

                    while (!string.IsNullOrEmpty(currentVictim.m_TreatmentTags))
                        yield return null;

                    this.SendVictimToEvac();
                }
            }

            nextThink = Time.time + thinkDuration;
            m_isThinking = false;
        }

        protected override void Cl_Think()
        {
            if (!m_isThinking && m_curLocation != null && Time.time > nextThink)
            {
                if (m_curLocation.GetVictims().Length > 0)
                {
                    StartCoroutine(Co_Think());
                }
            }
        }

        private AVictim[] sortVictimList(AVictim[] victimList)
        {
            AVictim[] sortedList = new AVictim[victimList.Length];
            int counter = 0;
            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P1)
                {
                    sortedList[counter] = victimList[i];
                    counter++;
                }
            }
            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P2)
                {
                    sortedList[counter] = victimList[i];
                    counter++;
                }
            }
            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P3)
                {
                    sortedList[counter] = victimList[i];
                    counter++;
                }
            }
            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P0)
                {
                    sortedList[counter] = victimList[i];
                    counter++;
                }
            }
            return sortedList;
        }

        public override void SV_AssumeControl(GameObject oldPlayerObject)
        {
            if (oldPlayerObject == null)
                return;

            base.SV_AssumeControl(oldPlayerObject);

            E3DFirstAidDocPlayer oldPlayer = oldPlayerObject.GetComponent<E3DFirstAidDocPlayer>();
            m_victimNetId = oldPlayer.m_victimNetId;
        }
    }
}
