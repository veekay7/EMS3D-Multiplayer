using TMPro;

namespace E3D
{
    public class GUIRefillItem : GUIBase
    {
        public TMP_Text m_ItemNameTxt;
        public TMP_Text m_PriceTxt;
        public TMP_Text m_NumStockTxt;
        public TMP_Text m_CurAmountTxt;
        public TMP_Text m_RefillAmountTxt;

        private ItemAttrib m_item = null;
        private int m_itemIndex = -1;
        private int m_refillAmount = 0;
        private int m_stockAmount = 0;
        private int m_totalCost = 0;

        //public E3DIncidentCmdrPlayer IncidentCmdr { get; private set; }

        public AFirstAidPoint FirstAidPoint { get; private set; }

        public int RefillAmount { get => m_refillAmount; }

        public int TotalCost { get => m_totalCost; }


        private void LateUpdate()
        {
            if (m_itemIndex != -1)
            {
                int curAmount = FirstAidPoint.ItemQuantities[m_itemIndex];
                //int stockAmount = IncidentCmdr.ItemQuantities[m_itemIndex];

                m_CurAmountTxt.text = curAmount.ToString();
                m_NumStockTxt.text = m_stockAmount.ToString();
                m_RefillAmountTxt.text = m_refillAmount.ToString();
            }
        }

        public void SetItem(/*E3DIncidentCmdrPlayer incidentCmdr,*/ AFirstAidPoint firstAidPoint, int itemIndex, int stockAmount)
        {
            //IncidentCmdr = incidentCmdr;
            FirstAidPoint = firstAidPoint;

            m_itemIndex = itemIndex;
            m_item = firstAidPoint.m_ItemAttribs[itemIndex];
            m_stockAmount = stockAmount;

            m_ItemNameTxt.text = m_item.m_PrintName;
            m_PriceTxt.text = m_item.Cost.ToString();
        }

        public void IncreaseAmount()
        {
            if (m_itemIndex == -1)
                return;

            if (m_stockAmount > 0)
            {
                m_refillAmount++;
                if (m_refillAmount > m_stockAmount)
                    m_refillAmount = 0;
            }

            m_totalCost = m_item.Cost * m_refillAmount;
        }

        public void DecreaseAmount()
        {
            if (m_itemIndex == -1)
                return;

            if (m_stockAmount > 0)
            {
                m_refillAmount--;
                if (m_refillAmount < 0)
                    m_refillAmount = m_stockAmount;
            }

            m_totalCost = m_item.Cost * m_refillAmount;
        }

        public void Refresh(int newStockAmount)
        {
            m_stockAmount = newStockAmount;
            m_refillAmount = 0;
            m_totalCost = 0;
        }
    }
}
