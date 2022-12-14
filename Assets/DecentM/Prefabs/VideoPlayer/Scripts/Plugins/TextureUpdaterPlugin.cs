using UnityEngine;

namespace DecentM.VideoPlayer.Plugins
{
    public sealed class TextureUpdaterPlugin : VideoPlayerPlugin
    {
        public Texture idleTexture;

        protected override void OnPlaybackStart(float duration)
        {
            Texture videoTexture = this.system.GetVideoTexture();

            this.SetTexture(videoTexture);
        }

        private void ShowIdleTexture()
        {
            this.SetTexture(idleTexture);
            this.SetAVPro(false);
        }

        private void SetAVPro(bool isAVPro)
        {
            foreach (ScreenHandler screen in this.system.screens)
            {
                screen.SetIsAVPro(isAVPro);
            }
        }

        protected override void OnAutoRetry(int attempt)
        {
            this.ShowIdleTexture();
        }

        protected override void OnVideoPlayerInit()
        {
            this.ShowIdleTexture();
        }

        protected override void OnUnload()
        {
            this.ShowIdleTexture();
        }

        protected override void OnPlaybackEnd()
        {
            this.ShowIdleTexture();
        }

        protected override void OnLoadReady(float duration)
        {
            this.SetAVPro(this.system.currentPlayerHandler.type == VideoPlayerHandlerType.AVPro);
        }

        protected override void OnPlayerSwitch(VideoPlayerHandlerType type)
        {
            Texture videoTexture = this.system.GetVideoTexture();

            this.SetTexture(videoTexture);
        }

        public void SetTexture(Texture texture)
        {
            if (texture == null)
                return;

            foreach (ScreenHandler screen in this.system.screens)
            {
                screen.SetTexture(texture);
            }

            this.events.OnScreenTextureChange();
        }
    }
}
