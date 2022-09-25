using UdonSharp;
using TMPro;

public class NewsItemRenderer : UdonSharpBehaviour
{
    public TextMeshProUGUI titleSlot;
    public TextMeshProUGUI dateSlot;
    public TextMeshProUGUI keywordsSlot;
    public TextMeshProUGUI contentSlot;
    public TextMeshProUGUI teaserSlot;
    public TextMeshProUGUI sourceSlot;

    public void SetData(
        string title,
        string date,
        string[] keywords,
        string content,
        string teaser,
        string source
    )
    {
        this.titleSlot.text = title;
        this.dateSlot.text = date;
        this.keywordsSlot.text = string.Join(", ", keywords);
        this.contentSlot.text = content;
        this.teaserSlot.text = teaser;
        this.sourceSlot.text = source;
    }

    public void SetData(NewsItem item)
    {
        this.titleSlot.text = item.title;
        this.dateSlot.text = item.date;
        this.keywordsSlot.text = string.Join(", ", item.keywords);
        this.contentSlot.text = item.content;
        this.teaserSlot.text = item.teaser;
        this.sourceSlot.text = item.source;
    }
}