namespace OmniVideo;

/// <summary>Exception type raised by <see cref="OmniVideoClient"/>.</summary>
public class OmniVideoException : Exception
{
    /// <summary>Business code returned by the API (200=ok, 0=biz fail), if any.</summary>
    public int? Code { get; }
    /// <summary>HTTP status code, if applicable.</summary>
    public int? Status { get; }

    public OmniVideoException(string message, int? code = null, int? status = null)
        : base(message)
    {
        Code = code;
        Status = status;
    }
}
