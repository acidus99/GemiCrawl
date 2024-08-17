
using Gemini.Net;
using Gemini.Net.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Kennedy.Scanner;

public class DumbRequestor
{
    const int ResponseLineMaxLen = 1100;

    Stopwatch ConnectTimer = new Stopwatch();
    Stopwatch DownloadTimer = new Stopwatch();
    Stopwatch AbortTimer = new Stopwatch();

    /// <summary>
    /// Amount of time, in ms, to wait before aborting the request or download
    /// </summary>
    public int AbortTimeout { get; set; } = 30 * 1000;

    /// <summary>
    /// Amount of time, in ms, to wait before aborting the request or download
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30 * 1000;

    /// <summary>
    /// Maximum amount of data to download for the response body, before aborting
    /// </summary>
    public int MaxResponseSize { get; set; } = 5 * 1024 * 1024;

    public string GetResponseLine(string url)
        => GetResponseLine(new GeminiUrl(url));

    public string GetResponseLine(GeminiUrl url)
        => doRequest(url, null);

    /// <summary>
    /// Make a request to a specific IP Address
    /// </summary>
    /// <param name="url"></param>
    /// <param name="iPAddress"></param>
    /// <returns></returns>
    public string GetResponseLine(GeminiUrl url, IPAddress iPAddress)
        => doRequest(url, iPAddress);

    private string doRequest(GeminiUrl url, IPAddress? iPAddress)
    {
        if (!url.Url.IsAbsoluteUri)
        {
            throw new ApplicationException("Trying to request a non-absolute URL!");
        }

        AbortTimer.Reset();
        ConnectTimer.Reset();
        DownloadTimer.Reset();

        try
        {
            var sock = new TimeoutSocket();
            AbortTimer.Start();
            ConnectTimer.Start();

            using (TcpClient client = (iPAddress != null) ?
                sock.Connect(iPAddress, url.Port, ConnectionTimeout) :
                sock.Connect(url.Hostname, url.Port, ConnectionTimeout))
            {

                using (SslStream sslStream = new SslStream(client.GetStream(), false,
                    new RemoteCertificateValidationCallback(ProcessServerCertificate), null))
                {

                    sslStream.ReadTimeout = AbortTimeout;
                    sslStream.AuthenticateAsClient(url.Hostname);
                    ConnectTimer.Stop();

                    sslStream.Write(GeminiParser.CreateRequestBytes(url));
                    DownloadTimer.Start();

                    string respLine = ReadResponseLine(sslStream);
                    return respLine;
                }
            }
        }
        catch (Exception ex)
        {
            return $"{GeminiParser.ConnectionErrorStatusCode} {ex.Message.Trim()}";
        }
    }

    private bool ProcessServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        //TODO: TOFU logic and logic to store certificate that was received...
        return true;
    }

    private string ReadResponseLine(Stream stream)
    {
        var respLineBuffer = new List<byte>(ResponseLineMaxLen);
        byte[] readBuffer = { 0 };

        bool hasValidLineEnding = false;

        int readCount = 0;
        //the response line is at most (2 + 1 + 1024 + 2) characters long. (a redirect with the max sized URL)
        //read that much
        while (stream.Read(readBuffer, 0, 1) == 1)
        {
            if (readBuffer[0] == (byte)'\r')
            {
                //spec requires a \n next
                stream.Read(readBuffer, 0, 1);
                if (readBuffer[0] != (byte)'\n')
                {
                    throw new Exception("Malformed Gemini header - missing LF after CR");
                }
                hasValidLineEnding = true;
                break;
            }
            //keep going if we haven't read too many
            readCount++;
            if (readCount > ResponseLineMaxLen)
            {
                throw new ApplicationException($"Invalid Gemini response line. Did not find \\r\\n within {ResponseLineMaxLen} bytes");
            }
            respLineBuffer.Add(readBuffer[0]);
            CheckAbortTimeout();
        }

        if (!hasValidLineEnding)
        {
            throw new ApplicationException($"Invalid Gemini response line. Did not find \\r\\n before connection closed");
        }

        //spec requires that the response line use UTF-8
        return Encoding.UTF8.GetString(respLineBuffer.ToArray());
    }

    private void CheckAbortTimeout()
    {
        if (AbortTimer.Elapsed.TotalMilliseconds > AbortTimeout)
        {
            throw new ApplicationException("Requestor abort timeout exceeded.");
        }
    }
}
