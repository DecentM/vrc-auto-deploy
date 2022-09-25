using UnityEngine;
using UdonSharp;
using TMPro;

public class NewsItemRenderer : UdonSharpBehaviour
{
    public Animator animator;

    private bool isExpanded
    {
        get { return this.animator.GetBool("IsExpanded"); }
        set { this.animator.SetBool("IsExpanded", value); }
    }

    public TextMeshProUGUI[] titleSlot;
    public TextMeshProUGUI[] dateSlot;
    public TextMeshProUGUI[] keywordsSlot;
    public TextMeshProUGUI[] contentSlot;
    public TextMeshProUGUI[] teaserSlot;
    public TextMeshProUGUI[] sourceSlot;

    private void SetSlot(TextMeshProUGUI[] slots, string value)
    {
        foreach (TextMeshProUGUI slot in slots)
        {
            slot.text = value;
        }
    }

    public void SetData(
        string title,
        string date,
        string[] keywords,
        string content,
        string teaser,
        string source
    )
    {
        this.SetSlot(this.titleSlot, title);
        this.SetSlot(this.dateSlot, date);
        this.SetSlot(this.keywordsSlot, string.Join(", ", keywords));
        this.SetSlot(this.contentSlot, content);
        this.SetSlot(this.teaserSlot, teaser);
        this.SetSlot(this.sourceSlot, source);
    }

    public void SetData(NewsItem item)
    {
        this.SetSlot(this.titleSlot, item.title);
        this.SetSlot(this.dateSlot, item.date);
        this.SetSlot(this.keywordsSlot, string.Join(", ", item.keywords));
        this.SetSlot(this.contentSlot, item.content);
        this.SetSlot(this.teaserSlot, item.teaser);
        this.SetSlot(this.sourceSlot, item.source);
    }

    public void SetState(bool expanded)
    {
        this.isExpanded = expanded;
    }

    public void OnReadMore()
    {
        this.SetState(true);
    }

    public void OnReadLess()
    {
        this.SetState(false);
    }
}