using System.Collections;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

namespace NewsProviders.GoogleNewsRSS
{
    public static class GoogleNewsProvider
    {
        private static string url = "https://news.google.com/rss";

        public static NewsResponse Parse(string response)
        {
            XElement xel = XElement.Parse(response);

            XElement channel = xel.Element("channel");

            NewsResponse result = new NewsResponse();

            IEnumerable<XElement> items = channel.Elements("item");

            result.items = new List<NewsResponseItem>();

            foreach (XElement item in items)
            {
                NewsResponseItem resultItem = new NewsResponseItem();

                resultItem.title = item.Element("title").Value;
                resultItem.date = item.Element("pubDate").Value;
                resultItem.keywords = new string[0];
                resultItem.content = string.Empty;
                resultItem.teaser = NewsFetcher.HtmlToPlainText(item.Element("description").Value);
                resultItem.source = $"{item.Element("source").Value}";

                result.items.Add(resultItem);
            }

            return result;
        }

        public static IEnumerator Get(Action<NewsResponse> OnSuccess)
        {
            return NewsFetcher.GetText(url, null, (string response) =>
            {
                OnSuccess(Parse(response));
            });
        }
    }
}
