﻿using System;
using System.Collections.Generic;
using Gemi.Net;
using GemiCrawler.Modules;
using System.IO;

namespace GemiCrawler.UrlFrontiers
{
    /// <summary>
    /// Manages our queue of URLs to crawl
    /// </summary>
    public class BalancedUrlFrontier : AbstractModule, IUrlFrontier
    {
        object locker;

        /// <summary>
        /// our queue of URLs to crawl
        /// </summary>
        PriorityQueue [] queues;

        int totalWorkerThreads;

        public BalancedUrlFrontier(int totalWorkers)
            : base("URL-FRONTIER")
        {
            locker = new object();
            totalWorkerThreads = totalWorkers;

            queues = new PriorityQueue[totalWorkerThreads];
            for(int i = 0; i< totalWorkerThreads; i++)
            {
                queues[i] = new PriorityQueue();
            }
        }

        private int queueForUrl(GemiUrl url)
        {
            return Math.Abs(url.Authority.GetHashCode()) % totalWorkerThreads;
        }
      
        public void AddUrl(GemiUrl url)
        {
            int queueID = queueForUrl(url);
            queues[queueID].AddUrl(url);
        }

        public int GetCount()
        {
            int totalCount = 0;
            for(int i=0;i<totalWorkerThreads; i++)
            {
                totalCount += queues[i].GetCount();
            }
            return totalCount;
        }

        public GemiUrl GetUrl(int crawlerID = 0)
            => queues[crawlerID].GetUrl();

        public override void OutputStatus(string outputFile)
        {
            File.AppendAllText(outputFile, CreateLogLine($"Total Queue Size: {GetCount()}\n"));
        }
    }
}
