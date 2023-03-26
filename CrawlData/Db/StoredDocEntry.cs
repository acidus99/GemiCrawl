﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

using Gemini.Net;
using Kennedy.Data;

namespace Kennedy.CrawlData.Db
{
    [Table("Documents")]
    [Index(nameof(Status))]
    public class StoredDocEntry
    {
        /// <summary>
        /// the ID we are using in the DB for the DocID. DocID is a ulong,
        /// but Sqlite3 doesn't support UInt64s, so we use a Int64 here and doing
        /// some unchecked casting with overflow to handle it
        /// </summary>
        [Key]
        [Column("DBDocID")]
        public long UrlID { get; set; }

        public DateTime FirstSeen { get; set; }

        public DateTime? LastVisit { get; set; }

        public DateTime? LastSuccessfulVisit { get; set; }

        public int ErrorCount { get; set; } = 0;

        [MaxLength(1024)]
        [Required]
        public string Url { get; set; }

        [NotMapped]
        public GeminiUrl GeminiUrl
        {
            get
            {
                if (geminiUrl == null)
                {
                    geminiUrl = new GeminiUrl(Url);
                }
                return geminiUrl;
            }
        }

        private GeminiUrl? geminiUrl = null;

        [Required]
        public string Domain { get; set; }

        [Required]
        public int Port { get; set; }

        public ConnectStatus ConnectStatus { get; set; } = ConnectStatus.Unknown;

        #region Things we get after fetching/parsing

        public int? Status { get; set; }

        /// <summary>
        /// everything after the status code
        /// </summary>
        public string Meta { get; set; }

        /// <summary>
        /// Did we deliberately skip downloading this body?
        /// </summary>
        public bool BodySkipped { get; set; } = false;

        public bool BodySaved { get; set; } = false;
        public int BodySize { get; set; }
        public uint? BodyHash { get; set; }

        public int OutboundLinks { get; set; }

        /// <summary>
        /// Latency of the request/resp, in ms
        /// </summary>
        public int ConnectTime { get; internal set; }

        public int DownloadTime { get; internal set; }

        #endregion

        #region Computed Fields that make it easier to query

        public string Title { get; set; }

        public string MimeType { get; set; }

        public string Language { get; set; }

        public int LineCount { get; set; }

        public double PopularityRank { get; set; }

        public int ExternalInboundLinks { get; set; }

        public ContentType ContentType { get; set; }

        #endregion

    }
}
