using UdonSharp;
using VRC.SDKBase;

namespace DecentM.VideoPlayer.Plugins
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class GlobalSyncPlugin : VideoPlayerPlugin
    {
        public float diffToleranceSeconds = 2f;

        private float latency = 0.1f;

        [UdonSynced, FieldChangeCallback(nameof(progress))]
        private float _progress;

        public float progress
        {
            set
            {
                if (Networking.GetOwner(this.gameObject) == Networking.LocalPlayer)
                    return;

                float desiredProgress = value + this.latency;

                _progress = value;
                float localProgress = this.system.GetTime();
                float diff = desiredProgress - localProgress;

                // While paused we accept any seek input from the owner
                if (!system.IsPlaying())
                    this.system.Seek(value);
                // If diff is positive, the local player is ahead of the remote one
                if (diff > diffToleranceSeconds || diff < diffToleranceSeconds * -1)
                    this.system.Seek(desiredProgress);
            }
            get => _progress;
        }

        [UdonSynced, FieldChangeCallback(nameof(isPlaying))]
        private bool _isPlaying;

        public bool isPlaying
        {
            set
            {
                if (Networking.GetOwner(this.gameObject) == Networking.LocalPlayer)
                    return;

                _isPlaying = value;
                if (value)
                    this.system.StartPlayback(this.system.GetTime() + this.latency);
                else
                    this.system.PausePlayback();
            }
            get => _isPlaying;
        }

        [UdonSynced, FieldChangeCallback(nameof(url))]
        private VRCUrl _url;

        public VRCUrl url
        {
            set
            {
                if (Networking.GetOwner(this.gameObject) == Networking.LocalPlayer)
                    return;
                if (value == null || string.IsNullOrEmpty(value.ToString()))
                    return;

                _url = value;

                this.events.OnLoadApproved(value);
                this.system.LoadVideo(value);
            }
            get => _url;
        }

        protected override void OnProgress(float timestamp, float duration)
        {
            if (Networking.GetOwner(this.gameObject) != Networking.LocalPlayer)
                return;

            this._progress = timestamp;
            this.RequestSerialization();
        }

        public bool selfOwned
        {
            get
            {
                return Networking.GetOwner(this.gameObject) == Networking.LocalPlayer;
            }
        }

        protected override void OnLoadRequested(VRCUrl url)
        {
            if (url == null)
                return;

            if (!this.selfOwned)
            {
                this.events.OnLoadDenied(url, "Only the player owner can change the URL");
                return;
            }
        }

        protected override void OnLoadApproved(VRCUrl url)
        {
            if (url == null)
                return;

            this._url = url;
            this.RequestSerialization();
        }

        public void SyncUnload()
        {
            if (this.selfOwned)
                return;

            this.system.UnloadVideo();
        }

        protected override void OnUnload()
        {
            this.latency = 0;

            if (!this.selfOwned)
                return;

            this.SendCustomNetworkEvent(
                VRC.Udon.Common.Interfaces.NetworkEventTarget.All,
                nameof(this.SyncUnload)
            );

            this._url = null;
            this._isPlaying = false;
            this.RequestSerialization();
        }

        protected override void OnPlaybackStart(float timestamp)
        {
            if (!this.selfOwned)
                return;

            this._isPlaying = true;
            this.RequestSerialization();
        }

        protected override void OnPlaybackStop(float timestamp)
        {
            if (!this.selfOwned)
            {
                this.SyncPausedTime();
                return;
            }

            this._progress = timestamp;
            this._isPlaying = false;
            this.RequestSerialization();
        }

        public void SyncPausedTime()
        {
            if (this.selfOwned)
                return;

            // Continuously refine the latency by calculating the diff between the synced progress and the local progress
            // Next time that "play" is pressed, the stream will be offset by this amount
            float currentTime = this.system.GetTime();
            float diff = this.progress - currentTime;
            this.latency += diff;

            if (this.system.IsPlaying())
                this.system.PausePlayback(this.progress);
        }

        protected override void OnLoadReady(float duration)
        {
            if (this.selfOwned)
                return;

            if (this.isPlaying)
                this.system.StartPlayback();
        }

        private int currentOwnerId = 0;

        protected override void OnVideoPlayerInit()
        {
            VRCPlayerApi owner = Networking.GetOwner(this.gameObject);
            if (owner == null || !owner.IsValid())
                return;

            this.currentOwnerId = owner.playerId;
            this.events.OnOwnershipChanged(this.currentOwnerId, owner);
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player == null || !player.IsValid())
                return;

            this.events.OnOwnershipChanged(this.currentOwnerId, player);
            this.currentOwnerId = player.playerId;
        }

        private bool ownershipLocked = false;

        public override bool OnOwnershipRequest(
            VRCPlayerApi requestingPlayer,
            VRCPlayerApi requestedOwner
        )
        {
            if (this.ownershipLocked)
                return false;

            if (requestingPlayer == null || !requestingPlayer.IsValid())
                return false;
            if (requestedOwner == null || !requestedOwner.IsValid())
                return false;

            return requestingPlayer.playerId == requestedOwner.playerId;
        }

        protected override void OnOwnershipSecurityChanged(bool locked)
        {
            this.ownershipLocked = locked;
        }

        protected override void OnOwnershipRequested()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }

        public void SetSelfOwned()
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
    }
}
