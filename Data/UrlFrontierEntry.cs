using Gemini.Net;

namespace Kennedy.Data;

public class UrlFrontierEntry
{
    public int DepthFromSeed { get; set; } = 0;

    /// <summary>
    /// has this url ever been seen before?
    /// </summary>
    public bool IsNewUrl { get; set; } = false;

    public bool IsProactive { get; set; } = false;

    public bool IsRetry
        => RetryCount > 0;

    public bool IsRobotsLimited { get; set; } = false;

    public int Priority { get; set; } = 0;

    public int RetryCount { get; set; } = 0;

    public required GeminiUrl Url { get; set; }
}