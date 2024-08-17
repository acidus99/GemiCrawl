﻿using System.Collections.Concurrent;
using Gemini.Net;
using Kennedy.Data;
using Kennedy.Crawler.Crawling;
using Kennedy.SearchIndex;
using Kennedy.Warc;
using WarcDotNet;

namespace Kennedy.Crawler.Crawling;

/// <summary>
/// Holds responses and flushes them to a WARC file and the search index
/// </summary>
public class ResultsWriter
{
    const int MaxUninterestingFileSize = 10 * 1024;

    ConcurrentQueue<ParsedResponse> responses;
    ConcurrentQueue<GeminiUrl> skippedRequests;
    GeminiWarcCreator warcCreator;

    SearchStorageWrapper searchWrapper;

    public int Saved { get; private set; }

    public ResultsWriter(string warcDirectory, SearchStorageWrapper search)
    {
        searchWrapper = search;

        Saved = 0;
        responses = new ConcurrentQueue<ParsedResponse>();
        skippedRequests = new ConcurrentQueue<GeminiUrl>();
        warcCreator = new GeminiWarcCreator(warcDirectory + DateTime.Now.ToString("yyyy-MM-dd") + ".warc.gz");
        warcCreator.WriteWarcInfo(new WarcInfoFields
        {
            {"software", "Kennedy Crawler"},
            {"hostname", "kennedy.gemi.dev"},
            {"timestamp", DateTime.Now },
            {"operator", "Acidus"}
        });
    }

    public void AddResponse(ParsedResponse parsedResponse)
        => responses.Enqueue(parsedResponse);

    public void AddSkippedUrlResponse(GeminiUrl url, SkippedReason reason)
    {
        //Only URLs which were skipped because of Robots need to possibly be removed from the search index
        if(reason == SkippedReason.SkippedForRobots)
        {
            skippedRequests.Enqueue(url);
        }
    }

    public void Flush()
    {
        ParsedResponse? parsedResponse;
        while (responses.TryDequeue(out parsedResponse))
        {
            WriteResponseToSearchIndex(parsedResponse);
            WriteResponseToWarc(parsedResponse);
        }
        GeminiUrl? url;
        while (skippedRequests.TryDequeue(out url))
        {
            searchWrapper.RemoveUrl(url);
        }
    }

    public void Close()
    {
        Flush();
        warcCreator.Dispose();
        searchWrapper.FinalizeStores();
        searchWrapper.DoGlobalWork();
    }

    private void WriteResponseToWarc(GeminiResponse response)
    {
        GeminiResponse optimizedResponse = OptimizeForStoage(response);
        warcCreator.WriteSession(optimizedResponse);
        Saved++;
    }

    private void WriteResponseToSearchIndex(ParsedResponse parsedResponse)
    {
        searchWrapper.StoreResponse(parsedResponse);
    }

    private GeminiResponse OptimizeForStoage(GeminiResponse response)
    {
        if (!response.HasBody || response.MimeType == null)
        {
            return response;
        }

        if (response.MimeType.StartsWith("text/") || response.MimeType.StartsWith("image/"))
        {
            return response;
        }

        if (response.BodySize > MaxUninterestingFileSize)
        {
            response.BodyBytes = response.BodyBytes!.Take(MaxUninterestingFileSize).ToArray();
            response.IsBodyTruncated = true;
        }

        return response;
    }

}