using AgentFrameworkToolkit.Codex;
using Microsoft.Agents.AI;
using Secrets;

namespace AgentFrameworkToolkit.Tests.Providers;

#pragma warning disable AFT999 // Exercise the experimental Codex provider.

[Trait("Category", "Live")]
public class CodexLiveTests
{
    [Fact]
    public async Task RunAsyncUsesApiKeyWithoutLocalLoginAsync()
    {
        string apiKey = SecretsManager.GetSecrets().OpenAiApiKey;
        Assert.False(string.IsNullOrWhiteSpace(apiKey), "Configure OpenAiApiKey in the shared .NET User Secrets store.");
        DirectoryInfo directory = Directory.CreateTempSubdirectory("aft-codex-api-key-");
        try
        {
            string codexHome = Directory.CreateDirectory(Path.Combine(directory.FullName, "codex-home")).FullName;
            string workspace = Directory.CreateDirectory(Path.Combine(directory.FullName, "workspace")).FullName;
            CodexAgentFactory factory = new(new CodexConnection
            {
                ApiKey = apiKey,
                CodexHome = codexHome
            });
            await using CodexAgent agent = factory.CreateAgent(new CodexAgentOptions
            {
                WorkingDirectory = workspace,
                Model = "gpt-5.4",
                Permissions = CodexPermissions.ReadOnly,
                ApprovalPolicy = CodexApprovalPolicy.Never
            });
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            AgentSession session = await agent.CreateSessionAsync(timeout.Token);
            AgentResponse response = await agent.RunAsync("Reply with exactly: API_KEY_OK. Do not use tools.", session, cancellationToken: timeout.Token);

            Assert.Equal("API_KEY_OK", response.Text.Trim());
            Assert.False(string.IsNullOrWhiteSpace(session.GetThreadId()));
            Assert.False(File.Exists(Path.Combine(codexHome, "auth.json")), "API-key authentication should not create a persistent login.");
        }
        finally
        {
            // Codex can populate its home with read-only Git pack files.
            foreach (string file in Directory.EnumerateFiles(directory.FullName, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
            }
            directory.Delete(true);
        }
    }
}
