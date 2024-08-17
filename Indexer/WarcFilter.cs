using WarcDotNet;
using Kennedy.Crawler.Filters;
using Gemini.Net;

namespace Kennedy.Indexer;

public static class WarcFilter
{
    public static void CreateFilteredWArc(string inputWarc, string outputWarc, string configDirectory)
    {
        if (!File.Exists(inputWarc))
        {
            throw new ArgumentException("File does not exist", nameof(inputWarc));
        }

        BlockListFilter denyList = new BlockListFilter(configDirectory);

        int read = 0;
        int write = 0;

        using (WarcWriter writer = new WarcWriter(outputWarc))
        {
            using (WarcReader reader = new WarcReader(inputWarc))
            {
                foreach (var record in reader)
                {
                    read++;
                    if(record is RequestRecord requestRecord)
                    {
                        GeminiUrl url = new GeminiUrl(requestRecord.TargetUri!);
                        if(!denyList.IsUrlAllowed(url).IsAllowed)
                        {
                            continue;
                        }
                    }

                    if (record is ResponseRecord responseRecord)
                    {
                        GeminiUrl url = new GeminiUrl(responseRecord.TargetUri!);
                        if (!denyList.IsUrlAllowed(url).IsAllowed)
                        {
                            continue;
                        }
                    }
                    write++;
                    writer.Write(record);
                }
            }
        }

        Console.WriteLine($"Read {read}. Wrote {write} records to '{outputWarc}'");
    }
}