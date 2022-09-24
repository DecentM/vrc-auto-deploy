namespace NewsProviders.NewsDataIo
{
    public struct NewsResult
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

    public struct NewsResponse
    {
        public string status;
        public int totalResults;
        public NewsResult[] results;
        public int nextPage;
    }
}