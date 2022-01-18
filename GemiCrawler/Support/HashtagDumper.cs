﻿using System;
using System.Text;
using System.Text.RegularExpressions;

using System.Linq;
using System.Collections.Generic;
using GemiCrawler.DocumentIndex.Db;
using GemiCrawler.DocumentStore;
using GemiCrawler.Utils;
using GemiCrawler.GemText;
using Gemi.Net;
using System.IO;
using GemiCrawler.DocumentIndex;


namespace GemiCrawler.Support
{
    public class HashtagDumper
    {
        DocIndexDbContext Db;
        TermTracker Tracker;
        string OutDir;

        public HashtagDumper(TermTracker tracker, string pathToCrawlDB)
        {
            Tracker = tracker;
            Db = new DocIndexDbContext(pathToCrawlDB);
        }


        public void GenerateFiles(string outputDir, int min)
        {
            OutDir = outputDir;

            Directory.CreateDirectory(outputDir);
            Directory.CreateDirectory(outputDir + "tags/");

            var terms = Tracker.GetSortedTerms(min);

            CreateTermPages(terms);
            CreatePopularIndexPage(terms);
            CreateAlphabIndexPage(terms);            
        }

        private string termPath(string term)
            => $"tags/{term}.gmi";


        private string getVariantString(List<string> variants)
        {
            var variout = $"@{variants[0]}";
            if (variants.Count > 1)
            {
                variout += $" (and @{variants[1]}";
                for (int i = 2; i < variants.Count; i++)
                {
                    variout += $", @{variants[i]}";
                }
                variout += ")";
            }
            return variout;
        }

        private void CreateTermPages(List<Tuple<string, int>> terms)
        {
            foreach (var term in terms)
            {
                var outfile = termPath(term.Item1);
                var variout = getVariantString(Tracker.GetVariations(term.Item1));

                StreamWriter fout = new StreamWriter(OutDir + outfile);
                fout.WriteLine($"# {term.Item1}");
                fout.WriteLine($"The hashtag #{term.Item1} appears on {term.Item2} pages in gemini space.");
                fout.WriteLine("Detected variants: " + variout);
                var urls = Tracker.GetOccurences(term.Item1);
                urls.Sort();
                foreach (var url in urls)
                {
                    var dbID = DocIndex.toLong(url.DocID);
                    var title = (Db.DocEntries.Where(x => x.DBDocID == dbID).Select(x => x.Title).FirstOrDefault());
                    title = title.Length > 0 ? title : $"{url.Hostname}{url.Path}";
                    fout.WriteLine($"=> {url.NormalizedUrl} {title}");
                }
                fout.Close();
            }
        }

        private void CreatePopularIndexPage(List<Tuple<string, int>> terms)
        {
            StreamWriter index = new StreamWriter(OutDir + "index.gmi");
            foreach (var term in terms)
            {
                var outfile = termPath(term.Item1);
                var variout = getVariantString(Tracker.GetVariations(term.Item1));

                index.WriteLine($"=> {outfile} {variout} ({term.Item2})");
            }
            index.Close();
        }
        private void CreateAlphabIndexPage(List<Tuple<string, int>> terms)
        {
            StreamWriter index = new StreamWriter(OutDir + "commonAZ.gmi");
            foreach (var term in terms.OrderBy(x => x.Item1))
            {
                var outfile = termPath(term.Item1);
                var variout = getVariantString(Tracker.GetVariations(term.Item1));

                index.WriteLine($"=> {outfile} {variout} ({term.Item2})");
            }
            index.Close();
        }
    }
}
