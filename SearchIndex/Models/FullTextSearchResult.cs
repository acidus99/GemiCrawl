﻿using System;
using Microsoft.EntityFrameworkCore;
using Gemini.Net;

namespace Kennedy.SearchIndex.Models
{
    [Keyless]
    public class FullTextSearchResult
    {
        public long UrlID { get; set; }

        public GeminiUrl Url { get; set; }
        public int BodySize { get; set; }
        public string Title { get; set; }
        public string Snippet { get; set; }

        public string Language { get; set; }
        public int LineCount { get; set; }

        public string Favicon { get; set; }

        public string Mime { get; set; }

        public int ExternalInboundLinks { get; set; }

        #region meta data for debugging

        //score of our FTS
        public double FtsRank { get; set; }
        //score of our popularity ranker
        public double PopRank { get; set; }
        public double TotalRank { get; set; }
        #endregion

    }
}
