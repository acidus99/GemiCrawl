﻿using System;

using Gemini.Net;
using Kennedy.Warc;

using System.Collections.Concurrent;
using Kennedy.SearchIndex.Storage;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Kennedy.Crawler
{
	/// <summary>
	/// Holds responses and flushes them to a WARC file
	/// </summary>
	public class ResultsWriter
	{
		ConcurrentQueue<GeminiResponse> responses;
		GeminiWarcCreator warcCreator;

		public int Saved { get; private set; }

		public ResultsWriter(string warcDirectory)
		{
			Saved = 0;
			responses = new ConcurrentQueue<GeminiResponse>();
			warcCreator = new GeminiWarcCreator(warcDirectory + "gemini.crawl.warc");
		}

		public void AddToQueue(GeminiResponse response)
		{
			responses.Enqueue(response);
		}

		public void Flush()
		{
			GeminiResponse? response;
			while(responses.TryDequeue(out response))
			{
				WriteResponseToWarc(response);
			}
		}

		private void WriteResponseToWarc(GeminiResponse response)
		{
			//was it truncated?
			if (ResponseWasTruncated(response))
			{
				warcCreator.RecordTruncatedSession(response);
			}
			else
			{
				warcCreator.RecordSession(response);
			}
            Saved++;
        }

		private bool ResponseWasTruncated(GeminiResponse response)
			=> response.ConnectStatus == ConnectStatus.Error && response.StatusCode != 49;
	}
}

