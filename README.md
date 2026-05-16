# OmniVideo.Sdk (.NET)

.NET client for [Omni Video](https://omnivideo.net/) — generate video and image content with the **Gemini Omni Video** series of models.

[Omni Video](https://omnivideo.net/) hosts the Gemini Omni Video family (`seedance-2` for text/image → video, `gpt-image-2` and `nano-banana-2` for text/image → image) behind one simple REST API.

## Install

```bash
dotnet add package OmniVideo.Sdk
```

Or in `.csproj`:

```xml
<PackageReference Include="OmniVideo.Sdk" Version="0.1.*" />
```

## Get an API key

Sign in at **<https://omnivideo.net/>**, open the account page, then create an `sk-…` token.

```bash
export OMNIVIDEO_API_KEY=sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

## Quick start

```csharp
using OmniVideo;

using var client = new OmniVideoClient(); // reads OMNIVIDEO_API_KEY

var task = await client.RunAsync(new CreateTaskInput
{
    ModelId     = "seedance-2",
    Prompt      = "a serene zen garden at sunrise, ultra detailed",
    AspectRatio = "16:9",
});

Console.WriteLine(task.OutputUrl); // VideoUrl or ImageUrl
```

### Lower level: create + poll

```csharp
using var client = new OmniVideoClient(apiKey: "sk-...");

var task = await client.CreateTaskAsync(new CreateTaskInput
{
    ModelId = "gpt-image-2",
    Prompt  = "cyberpunk corgi, neon rim light",
});

while (!task.IsDone)
{
    await Task.Delay(TimeSpan.FromSeconds(3));
    task = await client.GetTaskAsync(task.TaskId);
}

Console.WriteLine(task.ImageUrl);
```

## Models

| `ModelId`       | Modality           | Output      |
| --------------- | ------------------ | ----------- |
| `seedance-2`    | text/image → video | `VideoUrl`  |
| `gpt-image-2`   | text/image → image | `ImageUrl`  |
| `nano-banana-2` | text/image → image | `ImageUrl`  |

See the live model list and pricing on [omnivideo.net](https://omnivideo.net/).

## API

- `new OmniVideoClient(string? apiKey, string? baseUrl, HttpClient? httpClient)` — reads `OMNIVIDEO_API_KEY` if `apiKey` is null.
- `client.CreateTaskAsync(CreateTaskInput) → Task<TaskInfo>`
- `client.GetTaskAsync(string taskId) → Task<TaskInfo>`
- `client.RunAsync(CreateTaskInput, RunOptions?) → Task<TaskInfo>` — create + poll until terminal.
- `TaskInfo`: `TaskId`, `TaskStatus` (1=queued, 2=running, 3=success, 4=failed), `ImageUrl`, `VideoUrl`, `Credits`, `IsDone`, `OutputUrl`.
- Errors come back as `OmniVideoException` with `Code` and `Status` properties.

## Links

- Website & account: <https://omnivideo.net/>
- API docs: <https://omnivideo.net/api-docs>

## License

MIT
