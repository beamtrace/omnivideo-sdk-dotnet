# 把 `OmniVideo.Sdk` 发布到 NuGet

## 前置条件

1. 装好 .NET SDK 6.0+（macOS: `brew install --cask dotnet-sdk`）。
2. 到 <https://www.nuget.org/users/account/LogOn> 注册账号（可以用 Microsoft 账号或 GitHub OAuth）。
3. 在 <https://www.nuget.org/account/apikeys> 生成 **API key**：
   - **Key Name**: `omnivideo-publish`
   - **Expires**: 1 年
   - **Package Owner**: 选你自己
   - **Scopes**: 勾 `Push` → `Push new packages and package versions`
   - **Glob Pattern**: 首次发布用 `*`（之后可以收窄成 `OmniVideo.Sdk`）
   - 复制下来形如 `oy2xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`，**只显示一次**。
4. 包名 `OmniVideo.Sdk` 不能被人占用。检查 <https://www.nuget.org/packages/OmniVideo.Sdk>。

## 发布步骤

```bash
cd dotnet
dotnet pack -c Release           # 生成 bin/Release/OmniVideo.Sdk.0.1.0.nupkg
dotnet nuget push bin/Release/OmniVideo.Sdk.0.1.0.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

也可以用环境变量管理 key：

```bash
export NUGET_API_KEY=oy2xxxxxxxxxxxxxxxxxxxxxxxxxxx
dotnet nuget push bin/Release/*.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json
```

## 后续版本升级

修改 `.csproj` 里 `<Version>0.1.0</Version>`，重新 pack + push。已发布的版本不能覆盖（unlisted 可以但不删）。
