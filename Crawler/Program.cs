using Kennedy.Crawler.Crawling;
using Kennedy.Crawler.Filters;

namespace Kennedy.Crawler;

class Program
{
    static void Main(string[] args)
    {
        HandleArgs(args);

        var crawler = new WebCrawler(40, 6000000);

        crawler.AddUrlsFromWebDB();

        //crawler.AddSeed("gemini://mozz.us/");
        //crawler.AddSeed("gemini://kennedy.gemi.dev/observatory/known-hosts");
        //crawler.AddSeed("gemini://spam.works/");
        crawler.DoCrawl();

        return;
    }

    static void HandleArgs(string[] args)
    {
        if (args.Length == 1)
        {
            CrawlerOptions.OutputBase = args[0];
        }

        CrawlerOptions.OutputBase = ResolveHomePath(CrawlerOptions.OutputBase);
        ConfirmAndCreateDirectory(CrawlerOptions.OutputBase);

        CrawlerOptions.DocumentIndex = ResolveHomePath(CrawlerOptions.DocumentIndex);
        ConfirmAndCreateDirectory(CrawlerOptions.DocumentIndex);
    }

    static string ResolveHomePath(string path)
        => path.Contains("~/") ?
            path.Replace("~/", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + '/') :
            path;

    static void ConfirmAndCreateDirectory(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar))
        {
            path += Path.DirectorySeparatorChar;
        }
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}