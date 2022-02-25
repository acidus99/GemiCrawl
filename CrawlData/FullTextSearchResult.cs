﻿using System;
using Gemini.Net;

namespace Kennedy.CrawlData
{
    public class FullTextSearchResult
    {
        public GeminiUrl Url { get; set; }
        public int BodySize { get; set; }
        public string Title { get; set; }
        public string Snippet { get; set; }
        public long DBDocID { get; set; }

        public string Language { get; set; }
        public int LineCount { get; set; }

        public string Favicon { get; set; }

        public bool IsRecognizedLanguage
            => FormattedLanguage.Length > 0;

        public bool BodySaved { get; set; }

        #region meta data for debugging

        //score of our FTS
        public double FtsRank { get; set; }
        //score of our popularity ranker
        public double PopRank { get; set; }
        public double TotalRank { get; set; }
        #endregion

        public string FormattedLanguage
        {
            get {
                switch (Language)
                {
                    case "dan":
                        return "Danish";
                    case "deu":
                        return "German";
                    case "eng":
                        return "English";
                    case "fra":
                        return "French";
                    case "ita":
                        return "Italian";
                    case "jpn":
                        return "Japanese";
                    case "kor":
                        return "Korean";
                    case "nld":
                        return "Dutch";
                    case "nor":
                        return "Norwegian";
                    case "por":
                        return "Portuguese";
                    case "rus":
                        return "Russian";
                    case "spa":
                        return "Spanish";
                    case "swe":
                        return "Swedish";
                    case "zho":
                        return "Chinese";
                    default:
                        return "";
                }
            }
        }

    }
}
