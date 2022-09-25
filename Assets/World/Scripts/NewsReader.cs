
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class NewsReader : UdonSharpBehaviour
{
    public void OnNext()
    {

    }

    public void OnPrevious()
    {

    }

    public float itemSize = 1;

    public Transform cameraFollowTarget;
    public Transform instanceParent;
    public NewsStorage newsStorage;

    private void Start()
    {
        this.newsItemTemplate.SetActive(false);

        foreach (NewsItem item in this.newsStorage.newsItems)
        {
            this.InstantiateItem(item);
        }
    }

    public GameObject newsItemTemplate;

    private void InstantiateItem(NewsItem item)
    {
        GameObject newsObject = Instantiate(this.newsItemTemplate);
        NewsItemRenderer newsItemRenderer = newsObject.GetComponent<NewsItemRenderer>();

        if (newsItemRenderer == null)
            return;

        newsItemRenderer.SetData(item);
        newsObject.transform.SetParent(this.instanceParent, false);
        newsObject.transform.localPosition = new Vector3(0, 0, 0);
        newsObject.transform.localRotation = Quaternion.identity;
        newsObject.transform.localScale = new Vector3(1, 1, 1);

        newsObject.name = $"NewsItem_{item.name}";

        newsObject.SetActive(true);
    }
}
