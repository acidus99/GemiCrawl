﻿using System;
using Gemini.Net;

using Kennedy.Crawler.Utils;
using Kennedy.Data;

namespace Kennedy.Crawler.Frontiers
{
	public class DomainLimitFilter : IUrlFilter
	{
        int MaxHits;
        Bag<String> DomainHits;

		public DomainLimitFilter(int maxHits = 15000)
		{
            DomainHits = new Bag<string>();
            MaxHits = maxHits;
		}

        public bool IsUrlAllowed(UrlFrontierEntry entry)
        {
            int hits = DomainHits.Add(entry.Url.Authority);
            return (hits <= MaxHits);
        }
    }
}

