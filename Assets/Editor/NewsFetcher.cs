using System;
using System.Net.Http;
using System.Threading.Tasks;
using VRC.SDKBase.Editor.BuildPipeline;
using NewsProviders.NewsDataIo;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.Collections;
using DecentM.Shared;
using DecentM.EditorTools;
using UnityEngine.Networking;
using System.Linq;

public class NewsFetcher : IVRCSDKBuildRequestedCallback
{
    public int callbackOrder => 3;

    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    {
        return requestedBuildType == VRCSDKRequestedBuildType.Scene;
    }

    public static string newsUrl = string.Empty;

    [MenuItem("DecentM/World/Refresh News")]
    public static void FetchNews()
    {
        EditorCoroutine.Start(GetNewsCoroutine(newsUrl, OnNewsReceived));
    }

    private static IEnumerator GetJson<T>(string url, Action<T> OnSuccess)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.isHttpError || request.isNetworkError)
                yield return null;

            while (!request.downloadHandler.isDone)
                yield return new WaitForSeconds(0.25f);

            T response = JsonConvert.DeserializeObject<T>(request.downloadHandler.text);

            OnSuccess(response);
            yield return null;
        }
    }

    private static IEnumerator GetNewsCoroutine(string url, Action<NewsResponse> OnSuccess)
    {
        return GetJson(url, OnSuccess);
    }

    private static void OnNewsReceived(NewsResponse response)
    {
        NewsStorage storage = ComponentCollector<NewsStorage>.CollectOneFromActiveScene();

        if (storage == default(NewsStorage))
            return;

        foreach (NewsItem item in storage.newsItems)
        {
            GameObject.DestroyImmediate(item.gameObject);
        }

        storage.newsItems = new NewsItem[response.results.Length];

        for (int i = 0; i < response.results.Length; i++)
        {
            NewsResult result = response.results[i];
            NewsItem item = AddNewsItem(storage.transform, $"news_{i}");

            item.title = result.title;
            item.link = result.link;
            item.keywords = result.keywords;
            item.creator = result.creator;
            item.video_url = result.video_url;
            item.description = result.description;
            item.content = result.content;
            item.pubDate = result.pubDate;
            item.image_url = result.image_url;
            item.source_id = result.source_id;
            item.country = result.country;
            item.category = result.category;
            item.language = result.language;

            Inspector.SaveModifications(item);

            storage.newsItems[i] = item;
        }

        storage.refreshedAt = DateTime.UtcNow.ToFileTimeUtc();

        Inspector.SaveModifications(storage);
    }

    private static NewsItem AddNewsItem(Transform parent, string name)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Component.DestroyImmediate(obj.GetComponent<MeshRenderer>());
        Component.DestroyImmediate(obj.GetComponent<BoxCollider>());
        Component.DestroyImmediate(obj.GetComponent<MeshFilter>());

        obj.transform.SetParent(parent, false);

        obj.name = name;

        return obj.AddComponent<NewsItem>();
    }
}
