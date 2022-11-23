﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gemini.Net;

namespace Kennedy.Blazer
{
    public class DocumentStore
    {

        string pageStorageDir;

        public DocumentStore(string path)
        {
            pageStorageDir = path;
            if (Directory.Exists(pageStorageDir))
            {
                DirectoryInfo di = new DirectoryInfo(pageStorageDir);

                foreach (FileInfo file in di.EnumerateFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.EnumerateDirectories())
                {
                    dir.Delete(true);
                }

                Directory.Delete(pageStorageDir);
            }
        }


        public string GetStorageFilename(GeminiUrl url)
        {
            var filename = Path.GetFileName(url.Path);
            return (filename.Length > 0) ? filename : "index.gmi";
        }

        public string GetSavePath(GeminiUrl url)
        {
            var dir = GetStorageDirectory(url);
            var file = GetStorageFilename(url);
            return dir + file;
        }

        public string GetStorageDirectory(GeminiUrl url)
        {
            string hostDir = (url.Port == 1965) ? url.Hostname : $"{url.Hostname} ({url.Port})";

            string path = Path.GetDirectoryName(url.Path);
            if(string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            if(!path.EndsWith('/'))
            {
                path += "/";
            }

            return $"{pageStorageDir}{hostDir}{path}";
        }

        public bool Store(GeminiUrl url, GeminiResponse resp)
        {
            if(!resp.IsSuccess)
            {
                return true;
            }
            var dir = GetStorageDirectory(url);
            var file = GetStorageFilename(url);
            var path = dir + file;


            try
            {
                Directory.CreateDirectory(dir);
            } catch(Exception)
            { }

            try
            {
                //if for some reason the file already exists, don't do anything
                if (!File.Exists(path))
                {
                    File.WriteAllBytes(path, resp.BodyBytes);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
                
            return true;
        }

    }
}
