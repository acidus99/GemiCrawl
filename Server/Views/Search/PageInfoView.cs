﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Web;
using Microsoft.EntityFrameworkCore;

using Gemini.Net;
using Kennedy.SearchIndex.Web;
using Kennedy.SearchIndex.Search;
using Kennedy.SearchIndex.Models;
using Kennedy.Data;
using RocketForce;
using Kennedy.Archive.Db;

namespace Kennedy.Server.Views.Search
{
    internal class PageInfoView :AbstractView
    {
        public PageInfoView(GeminiRequest request, Response response, GeminiServer app)
            : base(request, response, app) { }

        WebDatabaseContext db = new WebDatabaseContext(Settings.Global.DataRoot);
        Document entry = null!;

        public override void Render()
        {
            long urlID = 0;

            Document? possibleEntry = null;

            var query = Request.Url.RawQuery;
            if (query.StartsWith("id=") && query.Length > 3)
            {
                try
                {
                    urlID = Convert.ToInt64(query.Substring(3));
                    possibleEntry = GetEntry(urlID);
                }
                catch (Exception)
                {
                }

            }
            else if (query.StartsWith("url=") && query.Length > 4)
            {
                try
                {
                    query = query.Substring(4);
                    query = HttpUtility.UrlDecode(query);
                    var url = new GeminiUrl(query);
                    possibleEntry = GetEntry(url.ID);
                }
                catch (Exception)
                { }
            }

            if (possibleEntry == null)
            {
                Response.Redirect("/");
                return;
            }
            entry = possibleEntry;

            Response.Success();

            Response.WriteLine($"# Page Info: {entry.GeminiUrl.Path}");
            Response.WriteLine($"=> {entry.Url} Visit Current Url");
            Response.WriteLine($"=> {RoutePaths.ViewUrlHistory(entry.GeminiUrl)} View archived copies with 🏎 DeLorean Time Machine");
            //var emoji = entry.Favicon?.Emoji + " " ?? "";
            var emoji = "";
            Response.WriteLine($"=> {entry.GeminiUrl.RootUrl} Capsule: {emoji}{entry.GeminiUrl.Hostname}");

            Response.WriteLine();
            Response.WriteLine($"## Metadata");
            Response.WriteLine($"* Mimetype: {entry.MimeType}");
            if (entry.Charset != null)
            {
                Response.WriteLine($"* Charset: {entry.Charset}");
            }

            Response.WriteLine($"* Size: {FormatSize(entry.BodySize)}");
            Response.WriteLine($"* First Seen: {entry.FirstSeen.ToString("yyyy-MM-dd")}");
            Response.WriteLine($"* Indexed on: {entry.LastSuccessfulVisit?.ToString("yyyy-MM-dd")}");

            RenderFileMetaData();

            RenderLinks();

        }

        private Document? GetEntry(long urlID)
        {
            return db.Documents
                .Where(x => x.UrlID == urlID)
                .Include(x => x.Image!)
                //.Include(x => x.Favicon)
                .FirstOrDefault()!;
        }

        private void RenderFileMetaData()
        {
            switch (entry.ContentType)
            {
                case ContentType.Gemtext:
                    RenderGemtextMetaData();
                    break;

                case ContentType.Image:
                    RenderImageMetaData();
                    break;
            }
        }

        private void RenderGemtextMetaData()
        {
            Response.WriteLine("### Gemtext Metadata");
            var title = entry.Title ?? "(Could not determine)";
            var language = (entry.Language != null) ? FormatLanguage(entry.Language) : "(Could not determine)";
            Response.WriteLine($"* Title: {title}");
            Response.WriteLine($"* Language: {language}");
            if (entry.LineCount != null)
            {
                Response.WriteLine($"* Lines: {entry.LineCount}");
            }
        }

        private void RenderImageMetaData()
        {
            if(entry.Image == null)
            {
                return;
            }
            
            Response.WriteLine("### Image Metadata");
            Response.WriteLine($"* Dimensions: {entry.Image.Width} x {entry.Image.Height}");
            Response.WriteLine($"* Format: {entry.Image.ImageType.ToUpper()}");

            var searchEngine = new SearchDatabase(Settings.Global.DataRoot);
            var terms = searchEngine.GetImageIndexText(entry.UrlID);

            if (terms != null)
            {
                Response.WriteLine($"* Indexable text:");
                Response.WriteLine($">{terms}");
            }
        }

        private void RenderLinks()
        {
            Response.WriteLine($"## Links");

            var tmplinks = (from links in db.Links
                                where links.TargetUrlID == entry.UrlID && !links.IsExternal
                                join docs in db.Documents on links.SourceUrlID equals docs.UrlID
                                orderby docs.Url
                                select new
                                LinkItem
                                {
                                    Url = docs.Url,
                                    Title = docs.Title,
                                    LinkText = links.LinkText
                                }).ToList();
            if (tmplinks.Count > 0)
            {
                Response.WriteLine($"### Internal Inbound Links");
                Response.WriteLine($"{tmplinks.Count} inbound links, from other pages on {entry.GeminiUrl.Hostname}.");
                RenderLinks(tmplinks, "From");
                Response.WriteLine();
            }

            tmplinks = (from links in db.Links
                        where links.TargetUrlID == entry.UrlID && links.IsExternal
                        join docs in db.Documents on links.SourceUrlID equals docs.UrlID
                        orderby docs.Url
                        select new
                        LinkItem
                        {
                            Url = docs.Url,
                            Title = docs.Title,
                            LinkText = links.LinkText
                        }).ToList();

            if (tmplinks.Count > 0)
            {
                Response.WriteLine($"### External Inbound Links");
                Response.WriteLine($"{tmplinks.Count} inbound links from other capsules.");
                RenderLinks(tmplinks, "From");
                Response.WriteLine();
            }

            if(entry.OutboundLinks > 0)
            {
                tmplinks = (from links in db.Links
                            where links.SourceUrlID == entry.UrlID
                            join docs in db.Documents on links.TargetUrlID equals docs.UrlID
                            select new LinkItem
                            {
                                Url = docs.Url,
                                Title = docs.Title,
                                LinkText = links.LinkText
                            }).ToList();

                Response.WriteLine($"### Outbound Links");
                Response.WriteLine($"{tmplinks.Count} outbound links from this page.");
                RenderLinks(tmplinks, "To");
                Response.WriteLine();
            }
        }

        private void RenderLinks(IEnumerable<LinkItem> links, string direction)
        {
            int counter = 0;
            if (links.Count() > 0)
            {
                foreach (var link in links)
                {
                    counter++;
                    Response.WriteLine($"=> {link.Url} {counter}. {FormatLink(direction, link.Url, link.Title, link.LinkText)}");
                }
            }
        }

        private string FormatLink(string direction, string url, string? pageTitle, string? linkText)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(direction);
            sb.Append(' ');
            if (pageTitle?.Length > 0)
            {
                sb.Append($"page titled '{pageTitle}'");
            }
            else
            {
                var targetUrl = new GeminiUrl(url);
                //only include hostname if it's a different capsule
                if (targetUrl.Hostname != entry.Domain)
                {
                    sb.Append(targetUrl.Hostname);
                }
                sb.Append(targetUrl.Path);
            }
            if(linkText?.Length > 0)
            {
                sb.Append($" with link '{linkText}'");
            }
            return sb.ToString();
        }

        private class LinkItem
        {
            public required string Url { get; set; }

            public string? Title { get; set; }

            public string? LinkText { get; set; }
        }
    }
}
