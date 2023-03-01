using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace E3D
{
    // events
    public class AreaEnterExitEvent : UnityEvent<AVictimPlaceableArea, AVictimPlaceableArea> { }

    public class ActorNetResponseEvent : UnityEvent<ActorNetResponse_s> { }

    public class ListChangedEvent<T> : UnityEvent<EListOperation, T, T> { }

    public class AreaVictimNumChangedEvent : UnityEvent<EListOperation, AVictim, AVictim> { }


    public struct ActorNetResponse_s
    {
        public string m_ResponseType;
        public string m_Message;
        public string m_Data;
    }


    // EMT base player class
    public class E3DEmtPlayerBase : E3DPlayer
    {
        [SerializeField, ReadOnlyVar]
        protected AVictimPlaceableArea m_curLocation = null;

        [SyncVar(hook = nameof(OnLocationNetIdChanged)), ReadOnlyVar]
        public uint m_curLocNetId = 0;

        public AreaEnterExitEvent onLocEnterExitFunc = new AreaEnterExitEvent();
        public AreaVictimNumChangedEvent onLocVictimNumChangedFunc = new AreaVictimNumChangedEvent();
        public ActorNetResponseEvent onResponseRecvFunc = new ActorNetResponseEvent();


        public AVictimPlaceableArea CurrentLocation
        {
            get
            {
                if (isServer)
                {
                    if (m_curLocNetId == 0)
                    {
                        m_curLocation = null;
                        return null;
                    }

                    if (m_curLocation == null)
                        m_curLocation = this.GetComponentFromNetId<AVictimPlaceableArea>(m_curLocNetId);

                    return m_curLocation;
                }

                return m_curLocation;
            }
        }

        [Client]
        public virtual void SetLocation(AVictimPlaceableArea newLocation)
        {
            var oldLocation = m_curLocation;

            if (oldLocation != null)
            {
                oldLocation.CMD_RemovePlayer(this.netIdentity);
                oldLocation.victimListChangedFunc -= OnLocVictimChanged;
            }

            if (onLocEnterExitFunc != null)
                onLocEnterExitFunc.Invoke(m_curLocation, newLocation);

            m_curLocation = newLocation;

            if (m_curLocation != null)
            {
                m_curLocation.CMD_AddPlayer(this.netIdentity);
                m_curLocation.victimListChangedFunc += OnLocVictimChanged;
                CMD_SetLocation(m_curLocation.netIdentity);
            }
            else
            {
                CMD_SetLocation(null);
            }
        }

        [Command(requiresAuthority = false)]
        private void CMD_SetLocation(NetworkIdentity locNetIdentity)
        {
            if (locNetIdentity != null)
                m_curLocNetId = locNetIdentity.netId;
            else
                m_curLocNetId = 0;
        }

        protected virtual void OnLocVictimChanged(EListOperation op, AVictim oldItem, AVictim newItem)
        {
            if (onLocVictimNumChangedFunc != null)
                onLocVictimNumChangedFunc.Invoke(op, oldItem, newItem);
        }

        [ClientRpc]
        protected void RPC_SendResponse(string type, string msg, string data)
        {
            SendResponse(type, msg, data);
        }

        protected void SendResponse(string type, string msg, string data)
        {
            if (onResponseRecvFunc != null)
            {
                ActorNetResponse_s response = new ActorNetResponse_s();

                response.m_ResponseType = type;
                response.m_Message = msg;
                response.m_Data = data;

                onResponseRecvFunc.Invoke(response);
            }
        }

        public override void SV_AssumeControl(GameObject oldPlayerObject)
        {
            if (oldPlayerObject == null)
                return;

            base.SV_AssumeControl(oldPlayerObject);

            E3DEmtPlayerBase oldPlayer = oldPlayerObject.GetComponent<E3DEmtPlayerBase>();
            m_curLocNetId = oldPlayer.m_curLocNetId;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            if (NetworkServer.active || NetworkClient.active)
            {
                if (m_curLocNetId != 0 && NetworkIdentity.spawned.ContainsKey(m_curLocNetId))
                {
                    var locNetIdentity = NetworkIdentity.spawned[m_curLocNetId];
                    if (locNetIdentity != null)
                    {
                        var loc = locNetIdentity.GetComponent<AVictimPlaceableArea>();
                        loc.SV_RemovePlayer(this);
                    }
                }
            }
        }

        protected virtual void OnLocationNetIdChanged(uint oldId, uint newId)
        {
            if (NetworkServer.active || NetworkClient.active)
            {
                if (isClient && !IsHumanPlayer)
                {
                    if (m_curLocation != null && m_curLocation.netId == newId)
                        return;

                    if (newId != 0)
                    {
                        m_curLocation = NetworkIdentity.spawned[newId].GetComponent<AVictimPlaceableArea>();
                        m_curLocation.AddPlayer(this);
                    }
                }
            }
        }
    }
}
