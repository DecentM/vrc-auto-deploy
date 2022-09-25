using System.Collections;
using System;
using System.Collections.Generic;

using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

namespace NewsProviders.DWGraphQL
{
    struct Keyword
    {
        public string id;
        public string name;
    }

    struct Article
    {
        public string id;
        public string title;
        public string teaser;
        public string date;
        public Keyword[] keywords;
        public string text;
    }

    struct ResponseData
    {
        public Article[] recent;
    }

    struct Response
    {
        public ResponseData data;
    }

    struct Query
    {
        public string query;
    }

    public static class DWNewsProvider
    {
        private static string url = "https://beta.dw.com/graphql";

        public static NewsResponse Parse(string response)
        {
            return JsonUtility.FromJson<NewsResponse>(response);
        }

        public static IEnumerator Get(Action<NewsResponse> OnSuccess)
        {
            string query = @"
                fragment WhatWeWant on Article {
	                id
	                title
	                teaser
	                date: creationDate
	                keywords {
		                id
		                name
	                }
	                text
                }

                query {
	                recent: mostRecentContent(lang: ENGLISH, amount: 25, types: [ARTICLE]){
		                ...WhatWeWant
	                }
                }
            ".Replace("\r\n", "\n").Replace("\t", "");

            Query queryBody = new Query();

            queryBody.query = query;

            string body = JsonConvert.SerializeObject(queryBody);

            Debug.Log(body);

            return NewsFetcher.PostJson(url, body, (Response response) =>
            {
                NewsResponse result = new NewsResponse();
                List<NewsResponseItem> items = new List<NewsResponseItem>();

                foreach (Article article in response.data.recent)
                {
                    NewsResponseItem item = new NewsResponseItem();

                    item.title = article.title;
                    item.teaser = article.teaser;
                    item.date = article.date;
                    item.keywords = article.keywords.Select((keyword) => keyword.name).ToArray();
                    item.content = NewsFetcher.HtmlToPlainText(article.text);
                    item.source = "Deutsche Welle";

                    items.Add(item);
                }

                result.items = items;

                OnSuccess(result);
            });
        }
    }
}
