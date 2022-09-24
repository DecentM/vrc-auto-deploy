using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace DecentM.Permissions
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerListEvents : UdonSharpBehaviour
    {
        private UdonSharpBehaviour[] subscribers;

        private void Start()
        {
            if (this.subscribers == null) this.subscribers = new UdonSharpBehaviour[0];
        }

        public int Subscribe(UdonSharpBehaviour behaviour)
        {
            bool initialised = this.subscribers != null;

            if (initialised)
            {
                UdonSharpBehaviour[] tmp = new UdonSharpBehaviour[this.subscribers.Length + 1];
                Array.Copy(this.subscribers, 0, tmp, 0, this.subscribers.Length);
                tmp[tmp.Length - 1] = behaviour;
                this.subscribers = tmp;
            }
            else
            {
                UdonSharpBehaviour[] tmp = new UdonSharpBehaviour[1];
                tmp[0] = behaviour;
                this.subscribers = tmp;
            }

            return this.subscribers.Length - 1;
        }

        public bool Unsubscribe(int index)
        {
            if (this.subscribers == null || this.subscribers.Length == 0 || index < 0 || index >= this.subscribers.Length) return false;

            UdonSharpBehaviour[] tmp = new UdonSharpBehaviour[subscribers.Length + 1];
            Array.Copy(this.subscribers, 0, tmp, 0, index);
            Array.Copy(this.subscribers, index + 1, tmp, index, this.subscribers.Length - 1 - index);
            this.subscribers = tmp;

            return true;
        }

        private void BroadcastEvent(string eventName, object[] data)
        {
            if (this.subscribers == null || this.subscribers.Length == 0) return;

            foreach (UdonSharpBehaviour subscriber in this.subscribers)
            {
                subscriber.SetProgramVariable($"OnPlayerListEvent_name", eventName);
                subscriber.SetProgramVariable($"OnPlayerListEvent_data", data);
                subscriber.SendCustomEvent("OnPlayerListEvent");
            }
        }

        #region Events

        public void OnPlayerAdded(string player)
        {
            this.BroadcastEvent(nameof(OnPlayerAdded), new object[] { player });
        }

        public void OnPlayerRemoved(string player)
        {
            this.BroadcastEvent(nameof(OnPlayerRemoved), new object[] { player });
        }

        public void OnSelfAdded()
        {
            this.BroadcastEvent(nameof(OnSelfAdded), new object[0]);
        }

        public void OnSelfRemoved()
        {
            this.BroadcastEvent(nameof(OnSelfRemoved), new object[0]);
        }

        public void OnPlayerJoinedOnList(VRCPlayerApi player)
        {
            this.BroadcastEvent(nameof(OnPlayerJoinedOnList), new object[] { player });
        }

        public void OnPlayerJoinedOffList(VRCPlayerApi player)
        {
            this.BroadcastEvent(nameof(OnPlayerJoinedOffList), new object[] { player });
        }

        public void OnPlayerLeftOnList(int playerId)
        {
            this.BroadcastEvent(nameof(OnPlayerLeftOnList), new object[] { playerId });
        }

        public void OnPlayerLeftOffList(int playerId)
        {
            this.BroadcastEvent(nameof(OnPlayerLeftOffList), new object[] { playerId });
        }

        #endregion
    }
}
