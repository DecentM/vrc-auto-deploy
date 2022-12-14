using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace DecentM.VideoPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoPlayerUI : UdonSharpBehaviour
    {
        public bool hasDefaultPlaylist = false;
        public string[] defaultPlaylist = new string[0];
    }
}
