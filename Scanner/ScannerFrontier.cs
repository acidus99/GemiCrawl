using Gemini.Net;
using Kennedy.Crawler.Utils;

namespace Kennedy.Scanner;

/// <summary>
/// Manages our queue of URLs to scan
/// </summary>
public class ScannerFrontier
{
    object locker;

    /// <summary>
    /// our queue of URLs to crawl
    /// </summary>
    public ScannerQueue[] queues;

    int totalWorkerThreads;
    ThreadSafeCounter totalUrls;

    public int Count => GetCount();

    public int Total => totalUrls.Count;

    public ScannerFrontier(int totalWorkers)
    {
        locker = new object();
        totalWorkerThreads = totalWorkers;
        totalUrls = new ThreadSafeCounter();

        queues = new ScannerQueue[totalWorkerThreads];
        for (int i = 0; i < totalWorkerThreads; i++)
        {
            queues[i] = new ScannerQueue();
        }
    }

    private int queueForUrl(GeminiUrl url)
    {
        //we are trying to avoid adding URLs that are all served by the same
        //system from being dumped into different buckets, where we then overwhelm
        //that server. Basically Flounder, since all the subdomains are served by the same system

        //try and look up the ip address for this host. If we don't get one,
        //fall back to using the hostname.

        string address = url.Authority;
        //string address = DnsCache.Global.GetLookup(url.Hostname);
        int hash = (address != null) ? address.GetHashCode() : url.Hostname.GetHashCode();

        return Math.Abs(hash) % totalWorkerThreads;
    }

    private int GetCount()
    {
        int totalCount = 0;
        for (int i = 0; i < totalWorkerThreads; i++)
        {
            totalCount += queues[i].Count;
        }
        return totalCount;
    }

    public GeminiUrl? GetUrl(int crawlerID)
        => queues[crawlerID].GetUrl();

    public void AddUrl(string url)
        => AddUrl(new GeminiUrl(url));

    public void AddUrl(GeminiUrl url)
    {
        totalUrls.Increment();
        int queueID = queueForUrl(url);
        queues[queueID].Add(url);
    }
}
