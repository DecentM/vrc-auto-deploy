using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace DecentM.VideoPlayer.Plugins
{
    public class AutoPlayPlugin : VideoPlayerPlugin
    {
        public bool autoplayOnLoad = true;

        private bool isOwner
        {
            get { return Networking.GetOwner(this.gameObject) == Networking.LocalPlayer; }
        }

        protected override void OnLoadReady(float duration)
        {
            if (this.isOwner)
                this.system.StartPlayback();
        }
    }
}
