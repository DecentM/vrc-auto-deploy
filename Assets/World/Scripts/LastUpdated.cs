
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using System;

public class LastUpdated : UdonSharpBehaviour
{
    public NewsStorage storage;
    public TextMeshProUGUI boldSlot;
    public TextMeshProUGUI normalSlot;

    void Start()
    {
        this.Render();
    }

    public void Render()
    {
        DateTime dt = DateTime.FromFileTimeUtc(this.storage.refreshedAt).ToLocalTime();
        string longDate = dt.ToLongDateString();
        string longTime = dt.ToLongTimeString();
        string relative = this.ToRelative(dt);

        this.boldSlot.text = $"Last Updated {relative}";
        this.normalSlot.text = $"{longDate}\n{longTime}";

        // Rerender in a minute to refresh the relative string
        this.SendCustomEventDelayedSeconds(nameof(Render), 60);
    }

    private int SECOND = 1;
    private int MINUTE = 60;
    private int HOUR = 60 * 60;
    private int DAY = 24 * 60 * 60;
    private int MONTH = 30 * 24 * 60 * 60;

    // https://stackoverflow.com/questions/11/calculate-relative-time-in-c-sharp
    private string ToRelative(DateTime dt)
    {
        var ts = new TimeSpan(DateTime.Now.Ticks - dt.Ticks);
        double delta = Math.Abs(ts.TotalSeconds);

        if (delta < 1 * MINUTE)
            return ts.Seconds == 1 ? "just now" : ts.Seconds + " seconds ago";

        if (delta < 2 * MINUTE)
            return "a minute ago";

        if (delta < 45 * MINUTE)
            return ts.Minutes + " minutes ago";

        if (delta < 90 * MINUTE)
            return "an hour ago";

        if (delta < 24 * HOUR)
            return ts.Hours + " hours ago";

        if (delta < 48 * HOUR)
            return "yesterday";

        if (delta < 30 * DAY)
            return ts.Days + " days ago";

        if (delta < 12 * MONTH)
        {
            int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
            return months <= 1 ? "one month ago" : months + " months ago";
        }
        else
        {
            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }
    }
}
