
using Gemini.Net;
namespace Kennedy.Scanner;

class Program
{
    static void Main(string[] args)
    {

        ScannerFrontier frontier = new ScannerFrontier(40);
        foreach(string line in File.ReadLines("/Users/billy/kennedy-capsule/crawl-data/all-urls.txt"))
        {
            frontier.AddUrl(line);
        }


        


        DumbRequestor dumbRequestor = new DumbRequestor();
        string x = dumbRequestor.GetResponseLine("gemini://gemini.conman.org/sigil");

        int xxx = 0;

    }

    private static readonly object _fileLock = new object();
    private static readonly string _filePath = "/Users/billy/kennedy-capsule/crawl-data/scanner.txt";

    public static void AppendTextToFile(string text)
    {
        lock (_fileLock)
        {
            File.AppendAllText(_filePath, text);
        }
    }
}

