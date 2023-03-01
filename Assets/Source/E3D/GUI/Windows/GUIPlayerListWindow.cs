using System;
using System.Collections.Generic;
using UnityEngine;

namespace E3D
{
    public class GUIPlayerListWindow : GUIBase
    {
        public GameObject m_PlayerListItemPrefab;
        public RectTransform m_Content;

        [HideInInspector]
        public E3DPlayer[] m_Players;
        [HideInInspector]
        public AVictimPlaceableArea m_Location;

        private List<GUIPlayerListItem> m_listItems = new List<GUIPlayerListItem>();


        private void OnEnable()
        {
            if (m_Players == null || m_Location == null)
                return;

            m_Location.playerListChangedFunc += M_Location_playerListChangedFunc;

            for (int i = 0; i < m_Players.Length; i++)
            {
                GameObject newItemObject = Instantiate(m_PlayerListItemPrefab);
                GUIPlayerListItem newItem = newItemObject.GetComponent<GUIPlayerListItem>();
                m_listItems.Add(newItem);

                newItem.m_RectTransform.SetParent(m_Content, false);
                newItem.SetPlayer(m_Players[i]);
                newItemObject.SetActive(true);
            }
        }

        private void M_Location_playerListChangedFunc(EListOperation op, E3DPlayer oldItem, E3DPlayer newItem)
        {
            Debug.Assert(m_Players != null);

            if (op == EListOperation.Add)
            {
                var found = Array.Find(m_Players, (p) => p == newItem);
                if (found == null)
                {
                    GameObject newPlayerListItemObject = Instantiate(m_PlayerListItemPrefab);
                    GUIPlayerListItem newPlayerListItem = newPlayerListItemObject.GetComponent<GUIPlayerListItem>();
                    m_listItems.Add(newPlayerListItem);

                    newPlayerListItem.m_RectTransform.SetParent(m_Content, false);
                    newPlayerListItem.SetPlayer(newItem);
                    newPlayerListItemObject.SetActive(true);
                }
            }
            else if (op == EListOperation.Remove)
            {
                int index = Array.IndexOf(m_Players, oldItem);
                Destroy(m_listItems[index].gameObject);
                m_listItems.RemoveAt(index);
            }
        }

        private void OnDisable()
        {
            if (m_Location != null)
                m_Location.playerListChangedFunc -= M_Location_playerListChangedFunc;

            m_Players = null;
            m_Location = null;

            for (int i = 0; i < m_listItems.Count; i++)
            {
                Destroy(m_listItems[i].gameObject);
            }
            m_listItems.Clear();
        }
    }
}
