
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class NewsItem : UdonSharpBehaviour
{
    public string title;
    public string link;
    public string[] keywords;
    public string[] creator;
    public string video_url;
    public string description;
    public string content;
    public string pubDate;
    public string image_url;
    public string source_id;
    public string[] country;
    public string[] category;
    public string language;
}
