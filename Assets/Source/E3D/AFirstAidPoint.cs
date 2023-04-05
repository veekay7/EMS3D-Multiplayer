using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace E3D
{
    public class AFirstAidPoint : AVictimPlaceableArea, ICostable
    {
        [Header("First Aid Point")]
        public int m_Cost = 0;
        public List<ItemAttrib> m_ItemAttribs = new List<ItemAttrib>();

        [ReadOnlyVar]
        public SyncList<int> m_itemQuantitiesSync = new SyncList<int>();

        public ItemAttrib[] ItemAttribs
        {
            get => m_ItemAttribs.ToArray();
        }

        public int NumItems
        {
            get => m_ItemAttribs.Count;
        }

        public SyncList<int> ItemQuantities
        {
            //get
            //{
            //    int[] itemQuantitiesCopy = new int[m_itemQuantitiesSync.Count];
            //    m_itemQuantitiesSync.CopyTo(itemQuantitiesCopy, 0);

            //    return itemQuantitiesCopy;
            //}

            get => m_itemQuantitiesSync;
        }

        public int Cost => m_Cost;


        protected override void Awake()
        {
            base.Awake();
            VehicleCanPass = false;
            MultiplePlayersAllowed = true;
        }

        public override void OnStartServer()
        {
            for (int itemSlot = 0; itemSlot < m_ItemAttribs.Count; itemSlot++)
            {
                if (m_ItemAttribs[itemSlot] != null)
                {
                    if (!m_ItemAttribs[itemSlot].m_IsInfinite)
                    {
                        m_itemQuantitiesSync.Add(m_ItemAttribs[itemSlot].m_DefaultCarry);
                    }
                    else
                    {
                        m_itemQuantitiesSync.Add(Consts.ITEM_INFINITE);
                    }
                }
                else
                {
                    m_itemQuantitiesSync[itemSlot] = -1;
                }
            }

            base.OnStartServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (E3DPlayer.Local != null)
            {
                if (!(E3DPlayer.Local is E3DFirstAidDocPlayer) && !(E3DPlayer.Local is E3DIncidentCmdrPlayer))
                    SetVisible(false);
            }
        }

        private void Start()
        {
            // calculate cost and put it on the game mode
            if (isServer)
            {
                for (int itemSlot = 0; itemSlot < m_ItemAttribs.Count; itemSlot++)
                {
                    if (m_itemQuantitiesSync[itemSlot] != Consts.ITEM_INFINITE)
                    {
                        if (isServer)
                            GameMode.Current.UseMoney(m_ItemAttribs[itemSlot].Cost * m_itemQuantitiesSync[itemSlot]);
                    }
                }
            }
        }

        [Command(requiresAuthority = false)]
        public void CMD_RefillItemQuantity(int itemSlot, int amount)
        {
            m_itemQuantitiesSync[itemSlot] += amount;
            GameMode.Current.UseMoney(m_ItemAttribs[itemSlot].m_Cost * amount);
        }

        [Command(requiresAuthority = false)]
        public void CMD_ConsumeItem(int itemSlot)
        {
            // reduce quantity if not infinite
            if (m_itemQuantitiesSync[itemSlot] != Consts.ITEM_INFINITE)
            {
                m_itemQuantitiesSync[itemSlot] -= 1;

                if (m_itemQuantitiesSync[itemSlot] < 0)
                    m_itemQuantitiesSync[itemSlot] = 0;
            }
        }

        public int GetSlot(ItemAttrib item)
        {
            return m_ItemAttribs.IndexOf(item);
        }

        public int GetItemQuantity(ItemAttrib item)
        {
            int itemSlot = m_ItemAttribs.IndexOf(item);
            if (itemSlot == -1)
                return -1;

            return m_itemQuantitiesSync[itemSlot];
        }

        public ItemAttrib GetItemAttrib(int itemSlot)
        {
            return m_ItemAttribs[itemSlot];
        }

        public int GetItemQuantity(int itemSlot)
        {
            return m_itemQuantitiesSync[itemSlot];
        }
    }
}
