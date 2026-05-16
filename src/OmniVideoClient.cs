using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniVideo;

/// <summary>
/// Client for the Omni Video API (<see href="https://omnivideo.net/"/>) — generate video and image
/// content with the Gemini Omni Video series of models.
/// </summary>
public class OmniVideoClient : IDisposable
{
    public const string DefaultBaseUrl = "https://omnivideo.net/api/v1";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null,
    };

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    /// <summary>
    /// Construct a client. If <paramref name="apiKey"/> is null, the <c>OMNIVIDEO_API_KEY</c>
    /// environment variable is read. Get a key from <see href="https://omnivideo.net/"/>.
    /// </summary>
    public OmniVideoClient(string? apiKey = null, string? baseUrl = null, HttpClient? httpClient = null)
    {
        var key = apiKey ?? Environment.GetEnvironmentVariable("OMNIVIDEO_API_KEY");
        if (string.IsNullOrEmpty(key))
        {
            throw new OmniVideoException(
                "Missing API key. Pass apiKey or set OMNIVIDEO_API_KEY. Get one at https://omnivideo.net/");
        }
        _apiKey = key;
        _baseUrl = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _ownsHttp = httpClient is null;
    }

    /// <summary>Submit a generation job.</summary>
    public async Task<TaskInfo> CreateTaskAsync(CreateTaskInput input, CancellationToken ct = default)
    {
        var doc = await SendAsync(HttpMethod.Post, "/tasks/create", input, ct).ConfigureAwait(false);
        return DeserializeTask(doc);
    }

    /// <summary>Fetch the current state of a task.</summary>
    public async Task<TaskInfo> GetTaskAsync(string taskId, CancellationToken ct = default)
    {
        var doc = await SendAsync(HttpMethod.Get, $"/tasks/{Uri.EscapeDataString(taskId)}", null, ct).ConfigureAwait(false);
        return DeserializeTask(doc);
    }

    /// <summary>Create a task and poll until it reaches a terminal state.</summary>
    public async Task<TaskInfo> RunAsync(CreateTaskInput input, RunOptions? options = null, CancellationToken ct = default)
    {
        var opts = options ?? new RunOptions();
        var task = await CreateTaskAsync(input, ct).ConfigureAwait(false);
        var deadline = DateTime.UtcNow + opts.MaxWait;
        while (!task.IsDone)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new OmniVideoException(
                    $"Task {task.TaskId} did not finish within {opts.MaxWait}",
                    code: task.TaskStatus);
            }
            await System.Threading.Tasks.Task.Delay(opts.PollInterval, ct).ConfigureAwait(false);
            task = await GetTaskAsync(task.TaskId, ct).ConfigureAwait(false);
        }
        if (task.TaskStatus == TaskInfo.StatusFailed)
        {
            throw new OmniVideoException(task.Msg ?? $"Task {task.TaskId} failed", code: TaskInfo.StatusFailed);
        }
        return task;
    }

    private async Task<JsonDocument> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, _baseUrl + path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
        {
            req.Content = JsonContent.Create(body, options: JsonOpts);
        }

        var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new OmniVideoException(
                "Unauthorized — check your OMNIVIDEO_API_KEY (https://omnivideo.net/).",
                status: 401);
        }

        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        JsonDocument doc;
        try
        {
            doc = string.IsNullOrEmpty(text)
                ? JsonDocument.Parse("{}")
                : JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            throw new OmniVideoException(
                $"Invalid JSON: {text[..Math.Min(text.Length, 200)]}",
                status: (int)resp.StatusCode);
        }

        var root = doc.RootElement;
        if (!resp.IsSuccessStatusCode)
        {
            var msg = TryGetString(root, "msg") ?? $"HTTP {(int)resp.StatusCode}";
            throw new OmniVideoException(msg, code: TryGetInt(root, "code"), status: (int)resp.StatusCode);
        }
        var bizCode = TryGetInt(root, "code");
        if (bizCode is not null && bizCode != 200)
        {
            throw new OmniVideoException(TryGetString(root, "msg") ?? "Business error", code: bizCode);
        }
        return doc;
    }

    private static TaskInfo DeserializeTask(JsonDocument doc)
    {
        var task = JsonSerializer.Deserialize<TaskInfo>(doc.RootElement.GetRawText())
            ?? throw new OmniVideoException("empty task payload");
        return task;
    }

    private static string? TryGetString(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object
            && el.TryGetProperty(name, out var p)
            && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;

    private static int? TryGetInt(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object
            && el.TryGetProperty(name, out var p)
            && p.ValueKind == JsonValueKind.Number
            && p.TryGetInt32(out var v)
                ? v
                : null;

    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Request body for <c>POST /tasks/create</c>.</summary>
public sealed class CreateTaskInput
{
    [JsonPropertyName("model_id")]   public required string ModelId    { get; init; }
    [JsonPropertyName("prompt")]     public required string Prompt     { get; init; }
    [JsonPropertyName("image_urls")] public List<string>?   ImageUrls  { get; init; }
    [JsonPropertyName("aspect_ratio")] public string?       AspectRatio{ get; init; }
}

/// <summary>Polling configuration for <see cref="OmniVideoClient.RunAsync"/>.</summary>
public sealed class RunOptions
{
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxWait      { get; init; } = TimeSpan.FromMinutes(10);
}
