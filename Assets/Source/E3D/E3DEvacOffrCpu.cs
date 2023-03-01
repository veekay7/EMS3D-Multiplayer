using System.Collections;
using UnityEngine;

namespace E3D
{
    public class E3DEvacOffrCpu : E3DEvacOffrPlayer
    {
        public override bool IsHumanPlayer { get => false; }

        private AVictim currentVictim;
        private AVictim[] sortedVictim;
        private AAmbulance availAmbulance;
        private float nextThink = 0;
        private float thinkDuration = 2;
        private bool m_isThinking;


        protected override void Cl_Think()
        {
            if (m_curLocation != null && m_evacPoint != null && !m_isThinking)
            {
                if (Time.time > nextThink)
                {
                    StartCoroutine(Co_DoThink());
                }
            }
        }

        private IEnumerator Co_DoThink()
        {
            availAmbulance = CheckAvailAmbulance(m_evacPoint.GetAmbulances());

            if (availAmbulance != null)
            {
                if (m_curLocation.GetVictims().Length > 0)
                {
                    SetAmbulance(availAmbulance);

                    while (CurrentAmbulance == null)
                        yield return null;

                    sortedVictim = sortVictimList(m_curLocation.GetVictims());
                    currentVictim = sortedVictim[0];

                    LoadVictimToAmbulance(currentVictim);

                    while (CurrentAmbulance.Victim == null)
                        yield return null;

                    //------------------------------------------------------------------------------
                    // VT: ookay, imma get the list of ambulance. it's quite simple really
                    var hospitals = GameState.Current.m_Hospitals;
                    int numHospitals = hospitals.Count;

                    // get a random one here
                    int rand_hospital_idx = UnityEngine.Random.Range(0, numHospitals);
                    AHospital selectedHospital = hospitals[rand_hospital_idx];

                    var route = GameState.Current.m_RouteController.GetRoute(selectedHospital.m_LocationId);
                    if (route == null)
                        Debug.Log("NO ROUTE FOUND BISH!");

                    SetHospitalToAmbulance(route);
                    //------------------------------------------------------------------------------

                    // VT: now we wait until the ambulance has all the shit we need
                    if (CurrentAmbulance == null)
                    {
                        Debug.Log("FUCK THIS STUPID!!");
                    }

                    while (!CurrentAmbulance.HasRoute)
                        yield return null;

                    EvacuateVictim();
                }
            }

            nextThink = Time.time + thinkDuration;
            m_isThinking = false;
        }


        private AAmbulance CheckAvailAmbulance(AAmbulance[] ambulanceList)
        {
            if (ambulanceList.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < ambulanceList.Length; i++)
            {
                if (!ambulanceList[i].InUse && !ambulanceList[i].HasRoute)
                    return ambulanceList[i];
            }
            return null;
        }

        private AVictim[] sortVictimList(AVictim[] victimList)
        {
            AVictim[] sortedList = new AVictim[victimList.Length];
            int counter = 0;
            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P1 && victimList[i].m_IsPregnant)
                {
                    sortedList[counter] = victimList[i];
                    counter++;
                }
            }

            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P1 && !victimList[i].m_IsPregnant)
                {
                    sortedList[counter] = victimList[i];
                    counter++;
                }
            }

            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P2 && victimList[i].m_IsPregnant)
                {
                    sortedList[counter] = victimList[i];
                    counter++;
                }
            }
            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P2 && !victimList[i].m_IsPregnant)
                {
                    sortedList[counter] = victimList[i];
                    counter++;
                }
            }
            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P3 && victimList[i].m_IsPregnant)
                {
                    sortedList[counter] = victimList[i];
                    counter++;
                }
            }
            for (int i = 0; i < victimList.Length; i++)
            {
                if (victimList[i].m_State.m_GivenPACS == EPACS.P3 && !victimList[i].m_IsPregnant)
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

            E3DEvacOffrPlayer oldPlayer = oldPlayerObject.GetComponent<E3DEvacOffrPlayer>();
            m_ambulanceNetId = oldPlayer.m_ambulanceNetId;
        }
    }
}
