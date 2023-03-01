using Mirror;
using UnityEngine;

namespace E3D
{
    public abstract class ALocationPoint : NetworkBehaviour
    {
        public string m_LocationId;
        [SyncVar]
        public string m_PrintName;
        public Sprite m_ThumbnailSprite;

        public bool VehicleCanPass { get; protected set; }

        protected virtual void Awake() { return; }

        public override void OnStartClient()
        {
            GameState.Current.AddLocation(this);
        }

        protected virtual void Reset()
        {
            m_LocationId = "default";
            m_PrintName = "Default";
            VehicleCanPass = true;
        }

        [Command(requiresAuthority = false)]
        public void CMD_SetPrintName(string newPrintname)
        {
            m_PrintName = newPrintname;
        }

        public override void OnStopClient()
        {
            GameState.Current.RemoveLocation(this);
        }

        protected virtual void OnDestroy() { return; }

        // editor only
        protected virtual void OnValidate() { return; }
    }
}
