using Mirror;
using UnityEngine;

namespace E3D
{
    public class E3DFirstAidDocPlayer : E3DEmtPlayerBase
    {
        public GameObject m_LarryModelPrefab;
        public GameObject m_BlackoutPrefab;
        public bool m_TreatmentWasApplied;

        private E3DLarryModel m_larryModel = null;
        private Canvas m_blackout = null;
        private FirstAidDocUI m_myUI = null;

        private AFirstAidPoint m_curfap = null;
        private float m_startTime = 0.0f;
        [SerializeField, ReadOnlyVar]
        private AVictim m_victim = null;

        [SyncVar(hook = nameof(OnVictimNetIdChanged)), ReadOnlyVar]
        public uint m_victimNetId = 0;


        public E3DLarryModel LarryModel { get => m_larryModel; }

        public Canvas Blackout { get => m_blackout; }

        public AFirstAidPoint CurrentFirstAidPoint { get => m_curfap; }

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

        public override void Pause()
        {
            if (m_myUI.ActiveView == m_myUI.m_CachedViews[FirstAidDocUI.APPLY_TREATMENT_SCREEN])
            {
                // don't do shit
                return;
            }

            base.Pause();
        }

        protected override void OnEscKeyPressed()
        {
            Pause();
        }

        protected override PlayerUIBase CreatePlayerUI()
        {
            if (m_larryModel == null)
            {
                GameObject larryModelObject = Instantiate(m_LarryModelPrefab);
                m_larryModel = larryModelObject.GetComponent<E3DLarryModel>();
            }

            if (m_blackout == null)
            {
                GameObject blackoutObject = Instantiate(m_BlackoutPrefab);
                m_blackout = blackoutObject.GetComponent<Canvas>();
            }

            m_larryModel.SetActive(false);

            m_blackout.worldCamera = CurrentCamera.m_UICam;
            m_blackout.gameObject.SetActive(false);

            GameObject newPlayerUIObject = Instantiate(m_PlayerUIPrefab);
            m_myUI = newPlayerUIObject.GetComponent<FirstAidDocUI>();

            return m_myUI;
        }

        public override void SetLocation(AVictimPlaceableArea area)
        {
            base.SetLocation(area);

            if (m_curLocation != null)
                m_curfap = (AFirstAidPoint)m_curLocation;
        }

        [Client]
        public void SetVictim(AVictim newVictim)
        {
            if (m_victim != null)
            {
                m_victim.Cl_Use(null);
                m_victim = null;

                m_startTime = 0.0f;
                m_TreatmentWasApplied = false;

                CMD_SetVictim(null);
            }

            if (newVictim != null)
            {
                newVictim.Cl_Use(this);

                m_victim = newVictim;
                m_startTime = Time.time;
                m_TreatmentWasApplied = false;

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
        public void UseItemOnVictim(int itemSlot)
        {
            if (CurrentVictim == null)
                return;

            m_TreatmentWasApplied = true;

            // get the item we wanna use from the FAP
            ItemAttrib item = m_curfap.GetItemAttrib(itemSlot);
            string itemName = item.m_PrintName.ToUpper();

            // check if the victim requires the item or not
            if (CurrentVictim.RequiresTreatment())
            {
                // consume one unit of that item
                m_curfap.CMD_ConsumeItem(itemSlot);

                // apply the item effect
                if (item.m_Effect != null)
                {
                    if (item.m_Effect.ApplyEffect(CurrentVictim))
                    {
                        CMD_CheckVictimAfterItemEffect(itemName);
                    }
                    else
                    {
                        SendResponse(
                        "doc_use_item",
                        "Used " + itemName + ".\nThere doesn't seem to be any effect.",
                        null);
                    }
                }
                else
                {
                    SendResponse(
                        "doc_use_item",
                        "Used " + itemName + ".\nERROR: No ItemEffect applied to item " + itemName,
                        null);
                }
            }
            else
            {
                SendResponse(
                    "doc_use_item",
                    "Used " + itemName + ".\nThe victim is already treated. There is no need to use " + itemName,
                    null);
            }
        }

        [Command]
        private void CMD_CheckVictimAfterItemEffect(string usedItemName)
        {
            string outputString = null;

            if (!string.IsNullOrEmpty(usedItemName))
                outputString += "Used " + usedItemName + ".\n";

            if (CurrentVictim.RequiresTreatment())
                outputString += "The victim seems to be more stable but more needs to be done.";
            else
                outputString += "The victim is now stable.";

            // update the treatment tally
            m_State.m_TotalTreatmentNum++;
            m_State.m_CorrectTreatmentNum++;

            RPC_SendResponse("doc_use_item", outputString, null);
        }

        [Client]
        public void SendVictimToMorgue()
        {
            if (CurrentVictim == null)
                return;

            if (!CurrentVictim.IsAlive)
            {
                // mark the selected victim as treated
                CurrentVictim.m_State.CMD_SetTreatedFlag(true);
                //CurrentVictim.m_IsActive = false;        // NOTE: this should not be set to false in multiplayer

                // do scoring shit here!!
                float duration = Time.time - m_startTime;
                m_startTime = 0.0f;
                GameMode.Current.ProcessMorgueScoring(this, m_victim, duration);

                SendResponse("send_morgue_success", "Sent " + m_victim.m_GivenName + " to the the morgue.", null);

                // remove victim from the first aid point
                CMD_RemoveAndTransferVictim();

                // unset the victim
                SetVictim(null);
            }
            else
            {
                SendResponse("not_dead", "The victim is still alive. You cannot send the victim to the morgue.", null);
            }
        }

        [Client]
        public void SendVictimToEvac()
        {
            if (CurrentVictim == null)
                return;

            if (!CurrentVictim.RequiresTreatment())
            {
                // mark the selected victim as treated
                CurrentVictim.m_State.CMD_SetTreatedFlag(true);

                // NOTE: deactivate the victim single player mode only!!
                CurrentVictim.m_IsActive = false;

                // do scoring shit here!!
                float duration = Time.time - m_startTime;
                m_startTime = 0.0f;

                GameMode.Current.ProcessTreatmentScoring(this, m_victim, duration);

                // notify listeners
                SendResponse("success", "Sent " + m_victim.m_GivenName + " to the evacuation area.", null);

                CMD_RemoveAndTransferVictim();

                // unset the victim
                SetVictim(null);
            }
            else
            {
                // the patient requirestreatment still
                SendResponse("require_treatment", "The victim still requires treatment.", null);
            }
        }

        [Command(requiresAuthority = false)]
        private void CMD_RemoveAndTransferVictim()
        {
            // remove victim from the first aid point and unset the victim from the 
            m_curfap.SV_RemoveVictim(CurrentVictim);

            // send the bloody victim to the evac point
            GameMode.Current.SendVictimToRandomEvacPoint(CurrentVictim);
        }

        protected override void UpdateMapView()
        {
            var casualtyPoints = GameState.Current.m_CasualtyPoints.ToArray();
            var evacPoints = GameState.Current.m_EvacPoints.ToArray();
            for (int i = 0; i < casualtyPoints.Length; i++)
            {
                casualtyPoints[i].SetVisible(false);
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
            Utils.SafeDestroyGameObject(m_larryModel);
            Utils.SafeDestroyGameObject(m_blackout);
            base.Cleanup();
        }
    }
}
