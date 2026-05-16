using System.Text.Json.Serialization;

namespace OmniVideo;

/// <summary>State of a generation job returned by the Omni Video API.</summary>
public sealed class TaskInfo
{
    public const int StatusQueued  = 1;
    public const int StatusRunning = 2;
    public const int StatusSuccess = 3;
    public const int StatusFailed  = 4;

    [JsonPropertyName("task_id")]     public string  TaskId     { get; init; } = "";
    [JsonPropertyName("task_status")] public int     TaskStatus { get; init; }
    [JsonPropertyName("image_url")]   public string? ImageUrl   { get; init; }
    [JsonPropertyName("video_url")]   public string? VideoUrl   { get; init; }
    [JsonPropertyName("credits")]     public int?    Credits    { get; init; }
    [JsonPropertyName("msg")]         public string? Msg        { get; init; }

    [JsonIgnore]
    public bool IsDone => TaskStatus == StatusSuccess || TaskStatus == StatusFailed;

    [JsonIgnore]
    public string? OutputUrl => VideoUrl ?? ImageUrl;
}
