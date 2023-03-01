﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace E3D
{
    public class VictimTreatmentHudView : HudBaseView, GUIItemListButton.IClickedHandler
    {
        [Header("Victim Treatment HUD View")]
        public GUIKrankenakte m_Karte;
        public Button m_BtnReturn;
        public Button m_BtnRequestRefill;
        public TMP_Text m_TxtSelectedItem;
        public GUIItemListButton m_ItemButtonPrefab;
        public RectTransform m_ItemButtonListTransform;

        private AVictim m_lastSetVictim;
        private AVictim m_curSetVictim;
        private List<GUIItemListButton> m_itemButtons = new List<GUIItemListButton>();


        protected override void Awake()
        {
            base.Awake();
            m_lastSetVictim = null;
            m_curSetVictim = null;
        }

        private void Update()
        {
            FirstAidDocUI fapHud = (FirstAidDocUI)Owner;

            m_curSetVictim = fapHud.FirstAidDocPlayer.CurrentVictim;

            if (m_curSetVictim != m_lastSetVictim)
            {
                if (Owner != null)
                {
                    m_lastSetVictim = m_curSetVictim;
                    m_Karte.SetVictim(m_curSetVictim);
                }
            }

            m_BtnReturn.interactable = !fapHud.FirstAidDocPlayer.m_TreatmentWasApplied;
        }

        public override void Open()
        {
            if (Owner != null)
            {
                FirstAidDocUI fapUI = (FirstAidDocUI)Owner;

                if (fapUI != null && fapUI.Player != null && fapUI.Player is E3DFirstAidDocPlayer)
                {
                    AFirstAidPoint fap = (AFirstAidPoint)((E3DFirstAidDocPlayer)fapUI.Player).CurrentLocation;

                    for (int i = 0; i < fap.ItemAttribs.Length; i++)
                    {
                        ItemAttrib item = fap.ItemAttribs[i];
                        
                        GUIItemListButton newBtn = Instantiate(m_ItemButtonPrefab);
                        newBtn.gameObject.SetActive(true);

                        newBtn.Owner = this.gameObject;
                        newBtn.ItemAttribute = item;
                        newBtn.FirstAidPoint = fap;
                        newBtn.ItemSlotIndex = i;

                        newBtn.m_RectTransform.SetParent(m_ItemButtonListTransform, false);

                        m_itemButtons.Add(newBtn);
                    }

                    if (m_itemButtons.Count > 0)
                        m_itemButtons[0].Button_Clicked();
                }
            }

            base.Open();
        }

        public override void Close()
        {
            for (int i = 0; i < m_itemButtons.Count; i++)
            {
                Destroy(m_itemButtons[i].gameObject);
            }
            m_itemButtons.Clear();

            base.Close();
        }

        public void ItemButton_Clicked(GUIItemListButton button)
        {
            if (Owner == null)
                return;

            m_TxtSelectedItem.text = button.ItemAttribute.m_PrintName;

            FirstAidDocUI fapHud = (FirstAidDocUI)Owner;
            if (fapHud != null)
            {
                fapHud.SelectItem(button.ItemSlotIndex);
            }
        }


        /// <summary>
        /// Item button clicked event
        /// </summary>
        public class ItemButtonClickedEvent : UnityEvent<GUIItemListButton> { }
    }
}
