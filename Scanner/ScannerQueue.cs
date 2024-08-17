using Gemini.Net;

namespace Kennedy.Scanner;

/// <summary>
/// 
/// </summary>
public class ScannerQueue
{
    object locker = new object();

    PriorityQueue<GeminiUrl, int> queue = new PriorityQueue<GeminiUrl, int>();

    /// <summary>
    /// tracks # of items we have had to a specific authority
    /// </summary>
    public Dictionary<string, int> AuthorityCounts = new Dictionary<string, int>();

    public void Add(GeminiUrl url)
    {
        var priority = GetPriority(url);
        lock (locker)
        {
            queue.Enqueue(url, priority);
        }
    }

    private int GetPriority(GeminiUrl url)
    {
        int value = AuthorityCounts.ContainsKey(url.Authority)
            ? AuthorityCounts[url.Authority] : 0;

        value++;
        AuthorityCounts[url.Authority] = value;
        return value;
    }

    public GeminiUrl? GetUrl()
    {
        GeminiUrl? ret = null;

        lock (locker)
        {
            ret = (queue.Count > 0) ?
                    queue.Dequeue() :
                    null;
        }
        return ret;
    }

    public int Count
        => queue.Count;
}