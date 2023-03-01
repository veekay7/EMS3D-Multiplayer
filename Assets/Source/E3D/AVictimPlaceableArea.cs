using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace E3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(CameraFacingBillboard), typeof(CameraRelativeScale))]
    public class AVictimPlaceableArea : ALocationPoint
    {
        [HideInInspector]
        public SpriteRenderer m_Renderer;
        [HideInInspector]
        public CameraFacingBillboard m_BillboardComponent;
        [HideInInspector]
        public CameraRelativeScale m_CameraRelativeScale;

        public Sprite m_BackgroundSprite;
        public bool m_SetVisible;

        // network vars
        [ReadOnlyVar]
        public SyncList<uint> m_playerNetIds = new SyncList<uint>();
        [ReadOnlyVar]
        public SyncList<uint> m_victimNetIds = new SyncList<uint>();

        // local vars
        [SerializeField, ReadOnlyVar]
        private List<E3DPlayer> m_players = new List<E3DPlayer>();
        [SerializeField, ReadOnlyVar]
        private List<AVictim> m_victimList = new List<AVictim>();

        public event ListChangedFuncDelegate<E3DPlayer> playerListChangedFunc;
        public event ListChangedFuncDelegate<AVictim> victimListChangedFunc;


        public bool IsVisible { get => m_Renderer.enabled; }

        public int NumPlayers { get => m_players.Count; }

        public int NumVictims { get => m_victimList.Count; }

        public bool MultiplePlayersAllowed { get; protected set; }


        protected override void Awake()
        {
            base.Awake();

            m_Renderer = GetComponent<SpriteRenderer>();
            m_BillboardComponent = GetComponent<CameraFacingBillboard>();
            m_CameraRelativeScale = GetComponent<CameraRelativeScale>();

            MultiplePlayersAllowed = true;
        }

        protected override void Reset()
        {
            base.Reset();
            m_SetVisible = true;
        }

        public override void OnStartClient()
        {
            SetVisible(true);

            for (int i = 0; i < m_victimNetIds.Count; i++)
            {
                if (m_victimNetIds[i] != 0)
                {
                    var victim = NetworkIdentity.spawned[m_victimNetIds[i]].GetComponent<AVictim>();
                    if (!m_victimList.Contains(victim))
                        m_victimList.Add(victim);
                }
            }

            m_playerNetIds.Callback += Callback_PlayerNetIdsModified;
            m_victimNetIds.Callback += Callback_VictimNetIdsModified;

            transform.localScale = Vector3.one;
            m_BillboardComponent.enabled = true;
            m_CameraRelativeScale.enabled = true;

            base.OnStartClient();
        }

        public void SetVisible(bool visible)
        {
            m_Renderer.enabled = visible;
        }


        #region Victim Management

        [Server]
        public void SV_AddVictim(AVictim newVictim)
        {
            if (!m_victimNetIds.Contains(newVictim.netId))
            {
                newVictim.m_StartHealth = newVictim.m_CurHealth;

                m_victimNetIds.Add(newVictim.netId);
            }
        }

        [Server]
        public void SV_RemoveVictim(AVictim victim)
        {
            if (m_victimNetIds.Contains(victim.netId))
            {
                m_victimNetIds.Remove(victim.netId);
            }
        }

        [Client]
        public void AddVictim(AVictim newVictim)
        {
            if (!m_victimList.Contains(newVictim))
            {
                m_victimList.Add(newVictim);

                if (victimListChangedFunc != null)
                    victimListChangedFunc.Invoke(EListOperation.Add, null, newVictim);
            }
        }

        [Client]
        public void RemoveVictim(AVictim victim)
        {
            if (m_victimList.Contains(victim))
            {
                m_victimList.Remove(victim);

                if (victimListChangedFunc != null)
                    victimListChangedFunc.Invoke(EListOperation.Remove, victim, null);
            }
        }

        [Command(requiresAuthority = false)]
        public void CMD_AddVictim(NetworkIdentity victimNetId)
        {
            if (victimNetId == null)
            {
                Debug.LogError("victimNetId is null");
                return;
            }

            AVictim victim = victimNetId.GetComponent<AVictim>();
            SV_AddVictim(victim);
        }

        [Command(requiresAuthority = false)]
        public void CMD_RemoveVictim(NetworkIdentity victimNetId)
        {
            if (victimNetId == null)
            {
                Debug.LogError("victimNetId is null");
                return;
            }

            AVictim victim = victimNetId.GetComponent<AVictim>();
            SV_RemoveVictim(victim);
        }

        public AVictim[] GetVictims()
        {
            return m_victimList.ToArray();
        }

        private void Callback_VictimNetIdsModified(SyncList<uint>.Operation op, int itemIndex, uint oldItem, uint newItem)
        {
            if (op == SyncList<uint>.Operation.OP_ADD)
            {
                // if you are doing this on the client side, then use NetworkClient.spawned, if doing on server side use NetworkIdentity.spawned.
                var newVictim = NetworkIdentity.spawned[newItem].GetComponent<AVictim>();
                AddVictim(newVictim);
            }
            else if (op == SyncList<uint>.Operation.OP_REMOVEAT)
            {
                var victim = NetworkIdentity.spawned[oldItem].GetComponent<AVictim>();
                RemoveVictim(victim);
            }
        }

        #endregion


        #region Player Management

        [Server]
        public void SV_AddPlayer(E3DPlayer newPlayer)
        {
            if (MultiplePlayersAllowed)
            {
                goto add_player;
            }
            else
            {
                if (NumPlayers == 0)
                    goto add_player;
            }

        add_player:
            if (!m_playerNetIds.Contains(newPlayer.netId))
                m_playerNetIds.Add(newPlayer.netId);
        }

        [Server]
        public void SV_RemovePlayer(E3DPlayer player)
        {
            if (m_playerNetIds.Contains(player.netId))
                m_playerNetIds.Remove(player.netId);
        }

        [Command(requiresAuthority = false)]
        public void CMD_AddPlayer(NetworkIdentity newPlayerNetId)
        {
            E3DPlayer newPlayer = newPlayerNetId.GetComponent<E3DPlayer>();
            SV_AddPlayer(newPlayer);
        }

        [Command(requiresAuthority = false)]
        public void CMD_RemovePlayer(NetworkIdentity playerNetId)
        {
            E3DPlayer player = playerNetId.GetComponent<E3DPlayer>();
            SV_RemovePlayer(player);
        }

        [Client]
        public void AddPlayer(E3DPlayer newPlayer)
        {
            if (!m_players.Contains(newPlayer))
            {
                m_players.Add(newPlayer);

                if (playerListChangedFunc != null)
                    playerListChangedFunc.Invoke(EListOperation.Add, null, newPlayer);
            }
        }

        [Client]
        public void RemovePlayer(E3DPlayer player)
        {
            if (m_players.Contains(player))
            {
                m_players.Remove(player);

                if (playerListChangedFunc != null)
                    playerListChangedFunc.Invoke(EListOperation.Remove, player, null);
            }
        }

        public E3DPlayer[] GetPlayers()
        {
            return m_players.ToArray();
        }

        private void Callback_PlayerNetIdsModified(SyncList<uint>.Operation op, int itemIndex, uint oldItem, uint newItem)
        {
            if (op == SyncList<uint>.Operation.OP_ADD)
            {
                // if you are doing this on the client side, then use NetworkClient.spawned, if doing on server side use NetworkIdentity.spawned.
                var newPlayer = NetworkIdentity.spawned[newItem].GetComponent<E3DPlayer>();
                AddPlayer(newPlayer);
            }
            else if (op == SyncList<uint>.Operation.OP_REMOVEAT)
            {
                if (NetworkIdentity.spawned.ContainsKey(oldItem))
                {
                    var playerNetIdentity = NetworkIdentity.spawned[oldItem];
                    if (playerNetIdentity != null)
                    {
                        var player = playerNetIdentity.GetComponent<E3DPlayer>();
                        RemovePlayer(player);
                    }
                    else
                    {
                        var players = m_players.ToArray();
                        for (int i = 0; i < players.Length; i++)
                        {
                            if (players[i] == null)
                            {
                                RemovePlayer(m_players[i]);
                            }
                            else
                            {
                                if (players[i].netId == oldItem)
                                    RemovePlayer(m_players[i]);
                            }
                        }
                    }
                }
                else
                {
                    var players = m_players.ToArray();
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (players[i] == null)
                        {
                            RemovePlayer(m_players[i]);
                        }
                        else
                        {
                            if (players[i].netId == oldItem)
                                RemovePlayer(m_players[i]);
                        }
                    }
                }
            }
        }

        #endregion


        public override void OnStopClient()
        {
            SetVisible(true);

            m_BillboardComponent.enabled = false;
            m_CameraRelativeScale.enabled = false;
            transform.localScale = Vector3.one;

            m_victimList.Clear();

            m_victimNetIds.Callback -= Callback_VictimNetIdsModified;
            m_playerNetIds.Callback -= Callback_PlayerNetIdsModified;

            base.OnStopClient();
        }

        // editor only
        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_Renderer == null)
            {
                m_Renderer = GetComponent<SpriteRenderer>();
                if (m_Renderer == null)
                    m_Renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (m_BillboardComponent == null)
            {
                m_BillboardComponent = GetComponent<CameraFacingBillboard>();
                if (m_BillboardComponent == null)
                    m_BillboardComponent = gameObject.AddComponent<CameraFacingBillboard>();
            }

            if (m_CameraRelativeScale == null)
            {
                m_CameraRelativeScale = GetComponent<CameraRelativeScale>();
                if (m_CameraRelativeScale == null)
                    m_CameraRelativeScale = gameObject.AddComponent<CameraRelativeScale>();
            }

            SetVisible(m_SetVisible);
        }
    }
}
