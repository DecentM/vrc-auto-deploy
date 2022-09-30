using UnityEngine;
using DecentM.VideoPlayer.Plugins;

public class GentleVideoplayer : VideoPlayerPlugin
{
    protected override void OnVideoPlayerInit()
    {
        this.system.SetVolume(0);
    }

    private float targetVolume = 0f;
    private int direction = 0;

    protected override void OnPlaybackStart(float timestamp)
    {
        this.targetVolume = 1f;
        this.direction = 1;
    }

    protected override void OnPlaybackEnd()
    {
        this.targetVolume = 0f;
        this.direction = -1;
    }

    protected override void OnPlaybackStop(float timestamp)
    {
        this.targetVolume = 0f;
        this.direction = -1;
    }

    protected override void OnUnload()
    {
        this.targetVolume = 0f;
        this.direction = -1;
    }

    protected override void OnProgress(float timestamp, float duration)
    {
        float remaining = duration - timestamp;

        // Add one second to let the transition fully finish, because we only get
        // OnProgress events once per second
        if (remaining < this.transitionSeconds + 1)
        {
            this.targetVolume = 0f;
            this.direction = -1;
        }
    }

    public float transitionSeconds = 1f;

    private void FixedUpdate()
    {
        if (this.direction == 0)
            return;

        float stepSize = (this.direction * (Time.fixedDeltaTime / 1));
        float newVolume = this.system.GetVolume() + stepSize;
        this.system.SetVolume(newVolume);

        switch (direction)
        {
            case 1:
                if (newVolume > this.targetVolume)
                    this.direction = 0;
                break;

            case -1:
                if (newVolume < this.targetVolume)
                    this.direction = 0;
                break;

            // Just in case
            default:
                this.direction = 0;
                return;
        }
    }
}
