using System;
using System.Diagnostics;
using Gemini.Net;
using Kennedy.SearchIndex.Models;
using Kennedy.SearchIndex.Web;
using Microsoft.EntityFrameworkCore;

namespace Kennedy.Crawler.Crawling;

internal class InitialUrlSelector : IDisposable
{
    private readonly WebDatabaseContext _context;
    private readonly IEnumerator<Document> _enumerator;


    public InitialUrlSelector()
    {
        _context = new WebDatabaseContext(CrawlerOptions.DocumentIndex);
        _enumerator = _context.Documents.AsNoTracking().AsEnumerable().GetEnumerator();
    }


    public InitialUrl Current = null!;

    public bool MoveNext()
    {

        while(_enumerator.MoveNext())
        {
            Document doc = _enumerator.Current;
            //TODO: add check for never visited URLs here
            if (doc.IsFeed)
            {
                Current = new InitialUrl(doc.GeminiUrl, 10);
                return true;
            }

            int age = Convert.ToInt32(DateTime.Now.Subtract(doc.LastVisit).TotalDays);

            if (IsGemtext(doc))
            {
                if (age > 30)
                {
                    Current = new InitialUrl(doc.GeminiUrl, 20);
                    return true;
                }
                //too new, continue;
                continue;
            }

            if (age > 90)
            {
                Current = new InitialUrl(doc.GeminiUrl, 50);
                return true;
            }

            //too new, continue
        }

        return false;
    }

    public void Dispose()
    {
        _enumerator.Dispose();
        _context.Dispose();
    }

    private bool IsGemtext(Document doc)
    {
        if(doc.StatusCode == 20 && doc.MimeType != null)
        {
            return doc.MimeType.StartsWith("text/gemini");
        }
        return false;
    }

    
}
[DebuggerDisplay("{Priority}, {Url}")]
public class InitialUrl
{
    public InitialUrl(GeminiUrl url, int priority)
    {
        Url = url;
        Priority = priority;
    }

    public int Priority { get; set; }
    public GeminiUrl Url { get; set; }
}

