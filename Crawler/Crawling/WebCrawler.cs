﻿using System.Diagnostics;
using Gemini.Net;
using Kennedy.Crawler.Frontiers;
using Kennedy.Crawler.Logging;
using Kennedy.Crawler.Protocols;
using Kennedy.Crawler.Utils;
using Kennedy.Data;
using Kennedy.Data.Parsers;

namespace Kennedy.Crawler.Crawling;

public class WebCrawler : IWebCrawler
{
    const int StatusIntervalScreen = 5000;

    int CrawlerThreads;
    ThreadSafeCounter TotalUrlsRequested;
    ThreadSafeCounter TotalUrlsProcessed;

    RejectedUrlLogger rejectionLogger;
    ResponseLogger responseLogger;

    int UrlLimit;

    bool HitUrlLimit
        => (TotalUrlsRequested.Count >= UrlLimit);

    IUrlFrontier UrlFrontier;
    UrlFrontierWrapper FrontierWrapper;

    ILinksFinder ResponseLinkFinder;
    ILinksFinder ProactiveLinksFinder;

    SeenContentTracker seenContentTracker;

    ResultsWriter ResultsWarc;

    Stopwatch CrawlerStopwatch;

    bool UserQuit;
    string StopFilePath;

    public bool LimitCrawlToSeeds
    {
        get { return FrontierWrapper.LimitCrawlToSeeds; }
        set { FrontierWrapper.LimitCrawlToSeeds = value; }
    }

    public WebCrawler(int crawlerThreads, int urlLimit)
    {
        CrawlerThreads = crawlerThreads;
        UrlLimit = urlLimit;

        ConfigureDirectories();
        LanguageDetector.ConfigFileDirectory = CrawlerOptions.ConfigDir;
        rejectionLogger = new RejectedUrlLogger(CrawlerOptions.RejectionsLog);
        responseLogger = new ResponseLogger(CrawlerOptions.ResponsesLog);

        TotalUrlsRequested = new ThreadSafeCounter();
        TotalUrlsProcessed = new ThreadSafeCounter();

        UrlFrontier = new BalancedUrlFrontier(CrawlerThreads);
        FrontierWrapper = new UrlFrontierWrapper(UrlFrontier, rejectionLogger);
        seenContentTracker = new SeenContentTracker();

        ProactiveLinksFinder = new ProactiveLinksFinder();
        ResponseLinkFinder = new ResponseLinkFinder();

        ResultsWarc = new ResultsWriter(CrawlerOptions.WarcDir);

        CrawlerStopwatch = new Stopwatch();

        UserQuit = false;
        StopFilePath = GetStopFilePath();
    }

    private string GetStopFilePath()
    {
        var info = new DirectoryInfo(".");
        return info.FullName + "/stop";
    }

    private void ConfigureDirectories()
    {
        Directory.CreateDirectory(CrawlerOptions.WarcDir);
        Directory.CreateDirectory(CrawlerOptions.Logs);
    }

    public void AddSeed(string url)
        => FrontierWrapper.AddSeed(new GeminiUrl(url));

    public void DoCrawl()
    {
        RobotsChecker.Global.Crawler = this;
        CrawlerStopwatch.Start();

        SpawnCrawlThreads();
        SpawnResultsWriter();

        int prevRequested = 0;
        do
        {
            CheckForQuit();
            Thread.Sleep(StatusIntervalScreen);

            int currRequested = TotalUrlsRequested.Count;
            string speed = ComputeSpeed((double)currRequested, (double)prevRequested, (double)StatusIntervalScreen);
            Console.WriteLine($"Elapsed: {CrawlerStopwatch.Elapsed}\tActive Workers: {WorkInFlight} Speed: {speed}\tTotal Requested: {currRequested}\tTotal Processed: {TotalUrlsProcessed.Count}\tRemaining: {UrlFrontier.Count}");
            prevRequested = TotalUrlsRequested.Count;

        } while (KeepWorkersAlive);

        Console.WriteLine("COMPLETE!");
        Console.WriteLine($"Elapsed: {CrawlerStopwatch.Elapsed}\tTotal Requested: {TotalUrlsRequested.Count}\tTotal Processed: {TotalUrlsProcessed.Count}\tRemaining: {UrlFrontier.Count}");
        FinalizeCrawl();
    }

    private void CheckForQuit()
    {
        //CheckForInteractiveQuit();
        CheckForFileQuit();
    }

    //private void CheckForInteractiveQuit()
    //{
    //    if (Console.In is StreamReader)
    //    {
    //        if (Console.KeyAvailable)
    //        {
    //            Console.WriteLine("stop? type 'quit'");
    //            Console.ReadKey(true);
    //            if (Console.ReadLine() == "quit")
    //            {
    //                Console.WriteLine("quiting...");
    //                UserQuit = true;
    //            }
    //            else
    //            {
    //                Console.WriteLine("resuming");
    //            }
    //        }
    //    }
    //}

    private void CheckForFileQuit()
    {
        if (File.Exists(StopFilePath))
        {
            UserQuit = true;
            File.Delete(StopFilePath);
            Console.WriteLine("Stop file detected. Stopping crawl");
        }
    }

    private void WritePendingResults()
    {
        do
        {
            Thread.Sleep(10000);
            if (KeepWorkersAlive)
            {
                ResultsWarc.Flush();
            }
        } while (KeepWorkersAlive);
    }

    private void FinalizeCrawl()
    {
        CrawlerStopwatch.Stop();
        rejectionLogger.Close();
        ResultsWarc.Close();
    }

    private void SpawnCrawlThreads()
    {
        for (int workerNum = 0; workerNum < CrawlerThreads; workerNum++)
        {
            var worker = new WebCrawlWorker(this, workerNum);
            var threadDelegate = new ThreadStart(worker.DoWork);
            var newThread = new Thread(threadDelegate);
            newThread.Name = $"Worker {workerNum}";
            newThread.Start();
        }
    }

    private void SpawnResultsWriter()
    {
        var threadDelegate = new ThreadStart(WritePendingResults);
        var newThread = new Thread(threadDelegate);
        newThread.Name = $"Results Writer";
        newThread.Start();
    }

    private string ComputeSpeed(double curr, double prev, double seconds)
    {
        double requestSec = (curr - prev) / seconds * 1000;
        return $"{requestSec.ToString("F1")} req / sec";
    }

    public void LogUrlRejection(GeminiUrl url, string rejectionType, string specificRule = "")
        => rejectionLogger.LogRejection(url, rejectionType, specificRule);

    public void ProcessRobotsResponse(GeminiResponse response)
    {
        TotalUrlsRequested.Increment();
        responseLogger.LogUrlResponse(response);
        ResultsWarc.AddToQueue(response);
        TotalUrlsProcessed.Increment();
    }

    public void ProcessRequestResponse(UrlFrontierEntry entry, GeminiResponse? response)
    {
        //null means it was ignored by robots
        if (response != null)
        {
            responseLogger.LogUrlResponse(response);
            ResultsWarc.AddToQueue(response);

            //record this url as now seen, and see if we have seen it before
            bool seenBefore = seenContentTracker.CheckAndRecord(response);

            //Don't parse proactive links for URLs. This can lead to bugs
            //e.g. a broken security.txt file that returns a gemtext doc, with relative links, creating a spider trap
            if (!seenBefore && !entry.IsProactive)
            {
                FrontierWrapper.AddUrls(entry.DepthFromSeed, ResponseLinkFinder.FindLinks(response));
            }
            //add proactive URLs
            FrontierWrapper.AddUrls(entry.DepthFromSeed, ProactiveLinksFinder.FindLinks(response), false);
        }
        TotalUrlsProcessed.Increment();
    }

    public UrlFrontierEntry? GetUrl(int crawlerID = 0)
    {
        if (HitUrlLimit || UserQuit)
        {
            return null;
        }

        var url = UrlFrontier.GetUrl(crawlerID);
        if (url != null)
        {
            TotalUrlsRequested.Increment();
        }
        return url;
    }

    /// <summary>
    /// Is there pending work in our queue?
    /// </summary>
    public bool HasUrlsToFetch
    {
        get
        {
            if (UserQuit)
            {
                return false;
            }
            return (!HitUrlLimit) ? (UrlFrontier.Count > 0) : false;
        }
    }

    /// <summary>
    /// Is there work being done
    /// </summary>
    public int WorkInFlight
        => TotalUrlsRequested.Count - TotalUrlsProcessed.Count;

    public bool KeepWorkersAlive
        => (HasUrlsToFetch || (WorkInFlight > 0));

}
