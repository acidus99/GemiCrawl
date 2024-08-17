using System;
using System.Collections.Generic;
using Gemini.Net;

namespace Kennedy.Scanner;

/// <summary>
/// Tracks if a 
/// </summary>
public class ConnectivityTracker
{
    Dictionary<string, ConnectivityInfo> hostConnectivity = new Dictionary<string, ConnectivityInfo>();

    public ConnectivityInfo GetConnectivityInfo(GeminiUrl url)
    {
        if(!hostConnectivity.ContainsKey(url.Authority))
        {
            ConnectivityInfo info = new ConnectivityInfo(url.Authority);
            hostConnectivity[url.Authority] = info;
            return info;
        }
        return hostConnectivity[url.Authority];
    }

}

public class ConnectivityInfo
{
    const int WindowSize = 10;

    /// <summary>
    /// what authority (host:port) is this associated with. Used purely for debugging
    /// </summary>
    public string Authority { get; private set; }
    public bool HasTerminalIssue { get; private set; }
    public string ErrorMessage { get; private set; } = "";


    /// <summary>
    /// tracks whether recent requests were successful or not
    /// </summary>
    Queue<bool> RequestWasSuccessful;

    public ConnectivityInfo(string authority)
    {
        Authority = authority;
        RequestWasSuccessful = new Queue<bool>();
    }

    public void RecordResponse(string responseLine)
    {
        //if we already have an issue, no need to continue
        if(HasTerminalIssue)
        {
            return;
        }

        if (IsTerminalResponse(responseLine))
        {
            HasTerminalIssue = true;
            ErrorMessage = responseLine;
            return;
        }

        RequestWasSuccessful.Enqueue(!responseLine.StartsWith("49 "));

        if (RequestWasSuccessful.Count > WindowSize)
        {
            RequestWasSuccessful.Dequeue();
        }
    }

    public bool HasTemporaryIssues()
    {
        float requestCount = RequestWasSuccessful.Count;

        //if we don't have enough data, its ok to try
        if (requestCount < WindowSize)
        {
            return false;
        }

        float errorCount = RequestWasSuccessful.Where(x => x == false).Count();

        return ((errorCount / requestCount) > 0.7);
    }

    /// <summary>
    /// Determines if a response is an error, such that future requests will be also fail.
    /// Examples include DNS errors, Connection refused, etc.
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    private bool IsTerminalResponse(string responseLine)
    {
        if (responseLine.StartsWith("49 "))
        {
            if (responseLine.Contains("Could not resolve hostname"))
                return true;

            if (responseLine.Contains("Connection refused"))
                return true;

            if (responseLine.Contains("Authentication failed, see inner exception"))
                return true;
        }
        return false;
    }
}
