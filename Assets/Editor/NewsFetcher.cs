using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using VRC.SDKBase.Editor.BuildPipeline;

using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.Collections;
using DecentM.EditorTools;
using UnityEngine.Networking;

using NewsProviders.DWGraphQL;
using System.Text.RegularExpressions;

public struct NewsResponseItem
{
    public string title;
    public string date;
    public string[] keywords;
    public string content;
    public string source;
    public string teaser;
}

public struct NewsResponse
{
    public List<NewsResponseItem> items;
}

public class NewsFetcher : IVRCSDKBuildRequestedCallback
{
    public int callbackOrder => 3;

    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    {
        return requestedBuildType == VRCSDKRequestedBuildType.Scene;
    }

    [MenuItem("DecentM/World/Refresh News")]
    public static void FetchNews()
    {
        Debug.Log("FetchNews()");

        EditorCoroutine.Start(DWNewsProvider.Get(OnNewsReceived));
    }

    private static IEnumerator MakeRequest(UnityWebRequest request, Action<UnityWebRequest> Enhancer, Action<string> OnSuccess)
    {
        request.SetRequestHeader("X-DecentM-Who-I-Am", " https://github.com/DecentM");
        request.SetRequestHeader("X-DecentM-Why-Querying", " To show an excerpt of the content inside virtual reality");
        request.SetRequestHeader("X-DecentM-Source-Code", " https://github.com/DecentM/vrc-auto-deploy");
        request.SetRequestHeader("X-DecentM-Contact", " decentm+news@decentm.com");

        Enhancer(request);

        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            Debug.LogError($"Request error: {request.error}");
            yield return null;
        }

        // Spin until we finish the request
        while (!request.isDone)
            yield return new WaitForSeconds(0.25f);

        OnSuccess(request.downloadHandler.text);
        yield return null;
    }

    public static IEnumerator GetText(string url, Action<UnityWebRequest> Enhancer, Action<string> OnSuccess)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        return MakeRequest(request, Enhancer, OnSuccess);
    }

    public static IEnumerator PostText(string url, string body, Action<UnityWebRequest> Enhancer, Action<string> OnSuccess)
    {
        UnityWebRequest request = UnityWebRequest.Post(url, body);
        return MakeRequest(request, Enhancer, OnSuccess);
    }

    public static IEnumerator GetJson<T>(string url, Action<T> OnSuccess)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);

        return MakeRequest(
            request,
            (r) => r.SetRequestHeader("Content-Type", "application/json"),
            (r) => OnSuccess(JsonConvert.DeserializeObject<T>(r))
        );
    }

    public static IEnumerator PostJson<T>(string url, string body, Action<T> OnSuccess)
    {
        UnityWebRequest request = UnityWebRequest.Post(url, body);
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        UploadHandler uh = new UploadHandlerRaw(bytes);
        request.uploadHandler = uh;

        return MakeRequest(
            request,
            (r) => r.SetRequestHeader("Content-Type", "application/json"),
            (r) => OnSuccess(JsonConvert.DeserializeObject<T>(r))
        );

    }

    private static void OnNewsReceived(NewsResponse response)
    {
        NewsStorage storage = ComponentCollector<NewsStorage>.CollectOneFromActiveScene();

        if (storage == default(NewsStorage))
            return;

        foreach (NewsItem item in storage.newsItems)
        {
            if (item == null || item.gameObject == null)
                continue;

            GameObject.DestroyImmediate(item.gameObject);
        }

        storage.newsItems = new NewsItem[response.items.Count];

        for (int i = 0; i < response.items.Count; i++)
        {
            NewsResponseItem result = response.items.ElementAt(i);
            NewsItem item = AddNewsItem(storage.transform, $"news_{i}");

            item.title = result.title;
            item.date = result.date;
            item.keywords = result.keywords;
            item.content = result.content;
            item.teaser = result.teaser;
            item.source = result.source;

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

    // https://www.codeproject.com/Articles/11902/Convert-HTML-to-Plain-Text-2
    public static string HtmlToPlainText(string source)
    {
        try
        {
            string result;

            // Remove HTML Development formatting
            // Replace line breaks with space
            // because browsers inserts space
            result = source.Replace("\r", " ");
            // Replace line breaks with space
            // because browsers inserts space
            result = result.Replace("\n", " ");
            // Remove step-formatting
            result = result.Replace("\t", string.Empty);
            // Remove repeating spaces because browsers ignore them
            result = Regex.Replace(result,
                                                                  @"( )+", " ");

            // Remove the header (prepare first by clearing attributes)
            result = Regex.Replace(result, @"<( )*head([^>])*>", "<head>", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(<( )*(/)( )*head( )*>)", "</head>", RegexOptions.IgnoreCase);
            result = Regex.Replace(result,
                     "(<head>).*(</head>)", string.Empty, RegexOptions.IgnoreCase);

            // remove all scripts (prepare first by clearing attributes)
            result = Regex.Replace(result, @"<( )*script([^>])*>", "<script>", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(<( )*(/)( )*script( )*>)", "</script>", RegexOptions.IgnoreCase);
            //result = Regex.Replace(result,
            //         @"(<script>)([^(<script>\.</script>)])*(</script>)",
            //         string.Empty,
            //         RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(<script>).*(</script>)", string.Empty, RegexOptions.IgnoreCase);

            // remove all styles (prepare first by clearing attributes)
            result = Regex.Replace(result, @"<( )*style([^>])*>", "<style>", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(<( )*(/)( )*style( )*>)", "</style>", RegexOptions.IgnoreCase);
            result = Regex.Replace(result,
                     "(<style>).*(</style>)", string.Empty, RegexOptions.IgnoreCase);

            // insert tabs in spaces of <td> tags
            result = Regex.Replace(result, @"<( )*td([^>])*>", "\t", RegexOptions.IgnoreCase);

            // insert line breaks in places of <BR> and <LI> tags
            result = Regex.Replace(result, @"<( )*br( )*>", "\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"<( )*li( )*>", "\n\t", RegexOptions.IgnoreCase);

            // insert line paragraphs (double line breaks) in place
            // if <P>, <DIV> and <TR> tags
            result = Regex.Replace(result, @"<( )*div([^>])*>", "\n\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"<( )*tr([^>])*>", "\n\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"<( )*p([^>])*>", "\n\n", RegexOptions.IgnoreCase);

            // Remove remaining tags like <a>, links, images,
            // comments etc - anything that's enclosed inside < >
            result = Regex.Replace(result, @"<[^>]*>", string.Empty, RegexOptions.IgnoreCase);

            // replace special characters:
            result = Regex.Replace(result, @" ", " ", RegexOptions.IgnoreCase);

            result = Regex.Replace(result, @"&bull;", " * ", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"&lsaquo;", "<", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"&rsaquo;", ">", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"&trade;", "(tm)", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"&frasl;", "/", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"&lt;", "<", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"&gt;", ">", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"&copy;", "(c)", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"&reg;", "(r)", RegexOptions.IgnoreCase);
            // Remove all others. More can be added, see
            // http://hotwired.lycos.com/webmonkey/reference/special_characters/
            result = Regex.Replace(result, @"&(.{2,6});", string.Empty, RegexOptions.IgnoreCase);

            // for testing
            //Regex.Replace(result,
            //       this.txtRegex.Text,string.Empty,
            //       RegexOptions.IgnoreCase);

            // make line breaking consistent
            result = result.Replace("\n", "\r");

            // Remove extra line breaks and tabs:
            // replace over 2 breaks with 2 and over 4 tabs with 4.
            // Prepare first to remove any whitespaces in between
            // the escaped characters and remove redundant tabs in between line breaks
            result = Regex.Replace(result, "(\r)( )+(\r)", "\r\r", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "(\t)( )+(\t)", "\t\t", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "(\t)( )+(\r)", "\t\r", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "(\r)( )+(\t)", "\r\t", RegexOptions.IgnoreCase);
            // Remove redundant tabs
            result = Regex.Replace(result, "(\r)(\t)+(\r)", "\r\r", RegexOptions.IgnoreCase);
            // Remove multiple tabs following a line break with just one tab
            result = Regex.Replace(result, "(\r)(\t)+", "\r\t", RegexOptions.IgnoreCase);
            // Initial replacement target string for line breaks
            string breaks = "\r\r\r";
            // Initial replacement target string for tabs
            string tabs = "\t\t\t\t\t";

            for (int index = 0; index < result.Length; index++)
            {
                result = result.Replace(breaks, "\n\n");
                result = result.Replace(tabs, "\t\t\t\t");
                breaks = breaks + "\n";
                tabs = tabs + "\t";
            }

            result = result.Replace("\r", "\n").Trim();

            // That's it.
            return result;
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            return source;
        }
    }
}
