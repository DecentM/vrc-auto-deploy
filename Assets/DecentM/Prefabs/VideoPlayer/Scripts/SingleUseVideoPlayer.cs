using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using DecentM.VideoPlayer;
using DecentM.VideoPlayer.Plugins;

public class SingleUseVideoPlayer : VideoPlayerPlugin
{
    public VRCUrl[] urls;

    private int currentIndex;

    private void NextUrl()
    {
        this.currentIndex++;
        if (this.currentIndex >= this.urls.Length)
            this.currentIndex = 0;
    }

    private VRCUrl GetUrl()
    {
        if (this.currentIndex >= this.urls.Length || this.currentIndex < 0)
            this.currentIndex = 0;

        return this.urls[this.currentIndex];
    }

    private void LoadUrl()
    {
#if UNITY_EDITOR && COMPILER_UDONSHARP
        return;
#endif

        if (this.system.currentPlayerHandler.type != VideoPlayerHandlerType.AVPro)
        {
            this.system.UnloadVideo();
            this.system.NextPlayerHandler();
        }

        VRCUrl url = this.GetUrl();
        this.system.RequestVideo(url);
    }

    protected override void OnVideoPlayerInit()
    {
        this.LoadUrl();
    }

    protected override void OnLoadReady(float duration)
    {
#if UNITY_EDITOR && COMPILER_UDONSHARP
        return;
#endif

        this.system.StartPlayback();
    }

    protected override void OnAutoRetryAbort()
    {
        this.system.UnloadVideo();
        this.NextUrl();
        this.LoadUrl();
    }

    protected override void OnPlaybackEnd()
    {
#if UNITY_EDITOR && COMPILER_UDONSHARP
        return;
#endif

        this.LoadUrl();
    }

    protected override void OnPlaybackStop(float timestamp)
    {
#if UNITY_EDITOR && COMPILER_UDONSHARP
        return;
#endif

        this.system.StartPlayback(timestamp);
    }
}
