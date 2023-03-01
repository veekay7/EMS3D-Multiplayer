using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace E3D
{
    public class GUIRefillItemWindow : GUIBase
    {
        public GUIRefillItem m_RefillItemSlotPrefab;
        public RectTransform m_SlotContentTransform;
        public TMP_Text m_TotalCostTxt;

        private List<GUIRefillItem> m_itemSlots = new List<GUIRefillItem>();

        public E3DIncidentCmdrPlayer IncidentCmdr { get; set; }

        public AFirstAidPoint FirstAidPoint { get; set; }


        private void OnEnable()
        {
            if (FirstAidPoint == null || IncidentCmdr == null)
                return;

            for (int itemIndex = 0; itemIndex < FirstAidPoint.NumItems; itemIndex++)
            {
                GUIRefillItem newItemSlot = Instantiate(m_RefillItemSlotPrefab);
                m_itemSlots.Add(newItemSlot);

                newItemSlot.m_RectTransform.SetParent(m_SlotContentTransform, false);
                newItemSlot.SetItem(/*IncidentCmdr, */FirstAidPoint, itemIndex, IncidentCmdr.ItemQuantities[itemIndex]);
                newItemSlot.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < m_itemSlots.Count; i++)
            {
                Destroy(m_itemSlots[i].gameObject);
            }
            m_itemSlots.Clear();

            FirstAidPoint = null;
            IncidentCmdr = null;
        }

        private void Update()
        {
            int totalCost = 0;
            for (int i = 0; i < m_itemSlots.Count; i++)
            {
                totalCost += m_itemSlots[i].TotalCost;
            }

            m_TotalCostTxt.text = totalCost.ToString();
        }

        public void Refill()
        {
            for (int i = 0; i < m_itemSlots.Count; i++)
            {
                if (!FirstAidPoint.m_ItemAttribs[i].m_IsInfinite)
                {
                    // if totally full already just skip to next item to fill
                    if (FirstAidPoint.ItemQuantities[i] == Consts.MAX_ITEMS)
                        continue;

                    IncidentCmdr.CMD_RemoveItemQuantity(i, m_itemSlots[i].RefillAmount);
                    FirstAidPoint.CMD_RefillItemQuantity(i, m_itemSlots[i].RefillAmount);
                    m_itemSlots[i].Refresh(IncidentCmdr.ItemQuantities[i]);
                }
            }
        }
    }
}

