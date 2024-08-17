using Kennedy.Crawler.Crawling;
using Kennedy.Crawler.Filters;

namespace Kennedy.Crawler;

class Program
{
    static void Main(string[] args)
    {
        HandleArgs(args);

        var crawler = new WebCrawler(40, 2000000);
        
        
        //crawler.AddUrlsFromWebDB();
        

        if (CrawlerOptions.SeedUrlsFile != "")
        {
            crawler.AddSeedsFromFile(CrawlerOptions.SeedUrlsFile);
        }
        else
        {
            crawler.AddSeed("gemini://mozz.us/");
            crawler.AddSeed("gemini://kennedy.gemi.dev/observatory/known-hosts");
            crawler.AddSeed("gemini://spam.works/");
        }
        crawler.DoCrawl();

        return;
    }

    static void HandleArgs(string[] args)
    {
        if (args.Length >= 1)
        {
            CrawlerOptions.OutputBase = args[0];
        }
        
        if (args.Length == 2)
        {
            if (!File.Exists(args[1]))
            {
                throw new FileNotFoundException("Could not locate seed url file", args[1]);
            }
            CrawlerOptions.SeedUrlsFile = args[1];
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