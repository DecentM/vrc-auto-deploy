using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class NewsItem : UdonSharpBehaviour
{
    public string title;
    public string date;
    public string[] keywords;
    public string content;
    public string teaser;
    public string source;
}
