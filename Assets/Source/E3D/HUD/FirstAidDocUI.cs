using UnityEngine;
using UnityEngine.UI;

namespace E3D
{
    public class FirstAidDocUI : PlayerUIBase
    {
        public const int VICTIM_SELECT_SCREEN = 0;
        public const int TREATMENT_SCREEN = 1;
        public const int APPLY_TREATMENT_SCREEN = 2;

        [Header("First Aid Doc HUD")]
        public Image m_ImgBackground;
        public Image m_ImgVictim;
        public GUIVictimCardList m_VictimCardList;

        private E3DFirstAidDocPlayer m_firstAidDoc;
        private int m_selItemSlot;

        public E3DFirstAidDocPlayer FirstAidDocPlayer { get => m_firstAidDoc; }

        public int SelectedItemIndex { get => m_selItemSlot; }


        protected override void Awake()
        {
            base.Awake();
            m_selItemSlot = -1;
        }

        protected override void Start()
        {
            base.Start();
            m_VictimCardList.onCardClickedFunc.AddListener(VictimCard_Clicked);
        }

        public override void SetPlayer(E3DPlayer newPlayer)
        {
            base.SetPlayer(newPlayer);

            if (m_firstAidDoc != null)
            {
                m_firstAidDoc.onLocEnterExitFunc.RemoveListener(Callback_AreaEnterExit);
                m_firstAidDoc.onLocVictimNumChangedFunc.RemoveListener(Callback_AreaVictimChanged);
                m_firstAidDoc.onResponseRecvFunc.RemoveListener(Callback_ResponseReceived);

                m_firstAidDoc = null;
            }

            if (Player != null)
            {
                m_firstAidDoc = (E3DFirstAidDocPlayer)Player;

                m_firstAidDoc.onLocEnterExitFunc.AddListener(Callback_AreaEnterExit);
                m_firstAidDoc.onLocVictimNumChangedFunc.AddListener(Callback_AreaVictimChanged);
                m_firstAidDoc.onResponseRecvFunc.AddListener(Callback_ResponseReceived);
            }
        }

        private void Callback_AreaEnterExit(AVictimPlaceableArea oldArea, AVictimPlaceableArea newArea)
        {
            if (oldArea != null && oldArea == m_firstAidDoc.CurrentLocation)
            {
                m_ImgBackground.sprite = null;
                m_ImgBackground.enabled = false;

                CloseCurrentView();

                m_firstAidDoc.CanMove = true;
            }

            if (newArea != null)
            {
                m_ImgBackground.sprite = newArea.m_BackgroundSprite;
                m_ImgBackground.enabled = true;

                OpenView(m_CachedViews[VICTIM_SELECT_SCREEN]);

                // create the victim cards
                m_VictimCardList.Clear();
                m_VictimCardList.CreateCards(newArea.GetVictims());

                m_firstAidDoc.CanMove = false;
            }
        }

        private void Callback_AreaVictimChanged(EListOperation op, AVictim oldVictim, AVictim newVictim)
        {
            if (m_firstAidDoc != null && m_firstAidDoc.CurrentLocation != null)
            {
                m_VictimCardList.Clear();
                m_VictimCardList.CreateCards(m_firstAidDoc.CurrentLocation.GetVictims());
            }
        }

        public void VictimCard_Clicked(GUIVictimCard card)
        {
            if (m_firstAidDoc == null)
                return;

            m_firstAidDoc.SetVictim(card.Victim);

            m_ImgVictim.sprite = card.Victim.m_PortraitSprite;
            m_ImgVictim.enabled = true;

            OpenView(m_CachedViews[TREATMENT_SCREEN]);
        }

        public void DeselectVictim()
        {
            if (m_firstAidDoc == null)
                return;

            m_firstAidDoc.SetVictim(null);

            m_ImgVictim.enabled = false;
            OpenView(m_CachedViews[VICTIM_SELECT_SCREEN]);
        }

        public void RequestRefillStock()
        {
            Debug.Log("Create the request refill stock function you dipshit!");
        }

        public void SendVictimToEvac()
        {
            if (m_firstAidDoc == null)
                return;

            m_firstAidDoc.SendVictimToEvac();

            // NOTE: view changes back to victim select screen after response is received
        }

        public void SendVictimToMorgue()
        {
            if (m_firstAidDoc == null)
                return;

            m_firstAidDoc.SendVictimToMorgue();

            // NOTE: view changes back to victim select screen after response is received
        }

        public void SelectItem(int slot)
        {
            if (m_firstAidDoc == null || slot == -1)
                return;

            m_selItemSlot = slot;
        }

        public void UseItem()
        {
            if (m_firstAidDoc.CurrentFirstAidPoint == null || m_selItemSlot == -1)
                return;

            // check if item has sufficient quantities!!!!
            AFirstAidPoint fap = m_firstAidDoc.CurrentFirstAidPoint;

            ItemAttrib item = fap.ItemAttribs[m_selItemSlot];
            int quantity = fap.ItemQuantities[m_selItemSlot];

            if (quantity > 0 || quantity == Consts.ITEM_INFINITE || item.m_IsInfinite)
            {
                // go to apply treatment screen!
                m_ImgBackground.enabled = false;
                m_ImgVictim.enabled = false;

                m_firstAidDoc.Blackout.gameObject.SetActive(true);
                m_firstAidDoc.LarryModel.gameObject.SetActive(true);

                OpenView(m_CachedViews[APPLY_TREATMENT_SCREEN]);
            }
            else
            {
                ShowTextBoxPrompt("Insufficient " + item.m_PrintName.ToUpper() + ".");
            }
        }

        public void ApplyTreatment()
        {
            if (m_selItemSlot == -1)
                return;

            m_firstAidDoc.UseItemOnVictim(m_selItemSlot);

            // NOTE: view changes back to treatment screen after response is received
        }

        public void CancelApplyTreatment()
        {
            m_selItemSlot = -1;

            m_ImgVictim.enabled = true;
            m_ImgBackground.enabled = true;

            if (m_firstAidDoc != null)
            {
                m_firstAidDoc.LarryModel.SetActive(false);
                m_firstAidDoc.Blackout.gameObject.SetActive(false);
            }

            OpenView(m_CachedViews[TREATMENT_SCREEN]);
        }

        private void Callback_ResponseReceived(ActorNetResponse_s response)
        {
            if (response.m_ResponseType.Equals("send_morgue_success") || response.m_ResponseType.Equals("success"))
            {
                Debug.Log(response.m_Message);

                ShowTextBoxPrompt(response.m_Message, () => {

                    m_ImgVictim.enabled = false;

                    OpenView(m_CachedViews[VICTIM_SELECT_SCREEN]);
                });
            }
            else if (response.m_ResponseType.Equals("doc_use_item"))
            {
                ShowTextBoxPrompt(response.m_Message, () => {
                    CancelApplyTreatment();
                });
            }
        }

        protected override void OnDestroy()
        {
            m_firstAidDoc.onLocEnterExitFunc.RemoveListener(Callback_AreaEnterExit);
            m_firstAidDoc.onLocVictimNumChangedFunc.RemoveListener(Callback_AreaVictimChanged);
            m_firstAidDoc.onResponseRecvFunc.RemoveListener(Callback_ResponseReceived);

            m_firstAidDoc = null;
        }
    }
}
