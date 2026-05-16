using System.Net;
using System.Text;
using OmniVideo;
using Xunit;

namespace OmniVideo.Tests;

public class StubHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode status, string body)> _responses;
    public List<HttpRequestMessage> Requests { get; } = new();

    public StubHandler(IEnumerable<(HttpStatusCode, string)> responses)
    {
        _responses = new Queue<(HttpStatusCode, string)>(responses);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add(request);
        if (_responses.Count == 0) throw new InvalidOperationException("no more stub responses");
        var (status, body) = _responses.Dequeue();
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}

public class ClientTests
{
    private OmniVideoClient MakeClient(params (HttpStatusCode, string)[] responses)
    {
        var http = new HttpClient(new StubHandler(responses));
        return new OmniVideoClient(apiKey: "sk-test", httpClient: http);
    }

    [Fact]
    public async Task CreateTask_returns_task_id()
    {
        var client = MakeClient(
            (HttpStatusCode.OK, "{\"code\":200,\"task_id\":\"abc\",\"task_status\":1}")
        );
        var t = await client.CreateTaskAsync(new CreateTaskInput { ModelId = "gpt-image-2", Prompt = "x" });
        Assert.Equal("abc", t.TaskId);
        Assert.Equal(TaskInfo.StatusQueued, t.TaskStatus);
    }

    [Fact]
    public void Missing_api_key_throws()
    {
        Environment.SetEnvironmentVariable("OMNIVIDEO_API_KEY", null);
        Assert.Throws<OmniVideoException>(() => new OmniVideoClient());
    }

    [Fact]
    public async Task Run_polls_to_success()
    {
        var client = MakeClient(
            (HttpStatusCode.OK, "{\"code\":200,\"task_id\":\"t1\",\"task_status\":1}"),
            (HttpStatusCode.OK, "{\"code\":200,\"task_id\":\"t1\",\"task_status\":2}"),
            (HttpStatusCode.OK, "{\"code\":200,\"task_id\":\"t1\",\"task_status\":3,\"image_url\":\"https://x/y.png\"}")
        );
        var task = await client.RunAsync(
            new CreateTaskInput { ModelId = "gpt-image-2", Prompt = "x" },
            new RunOptions { PollInterval = TimeSpan.Zero });
        Assert.Equal(TaskInfo.StatusSuccess, task.TaskStatus);
        Assert.Equal("https://x/y.png", task.OutputUrl);
    }

    [Fact]
    public async Task Business_error_raised()
    {
        var client = MakeClient(
            (HttpStatusCode.OK, "{\"code\":0,\"msg\":\"insufficient credits\"}")
        );
        var ex = await Assert.ThrowsAsync<OmniVideoException>(() =>
            client.CreateTaskAsync(new CreateTaskInput { ModelId = "gpt-image-2", Prompt = "x" }));
        Assert.Contains("insufficient credits", ex.Message);
    }
}
