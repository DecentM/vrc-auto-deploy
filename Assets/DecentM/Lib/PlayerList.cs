using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace DecentM
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerList : UdonSharpBehaviour
    {
        [Header("Settings")]
        [Tooltip("A list of player names")]
        public string[] players;

        public bool includeMaster = false;

        public bool CheckPlayer(VRCPlayerApi player)
        {
            if (player == null || !player.IsValid())
                return false;

            if (this.includeMaster && player.isMaster)
                return true;

            foreach (string playerName in this.players)
            {
                if (playerName == player.displayName)
                    return true;
            }

            return false;
        }

        public bool CheckPlayerByName(string player)
        {
            if (string.IsNullOrEmpty(player))
                return false;

            foreach (string playerName in this.players)
            {
                if (playerName == player)
                    return true;
            }

            return false;
        }
    }
}
