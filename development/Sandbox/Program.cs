using System.Text;
using AgentFrameworkToolkit.Tools;
using Sandbox.Providers;

#pragma warning disable CS8321 // Local function is declared but never used
Console.Clear();

Console.OutputEncoding = Encoding.UTF8;
//await Sandbox.Providers.AnthropicDemo.RunAsync();
await Sandbox.Providers.MicrosoftFoundry.RunAsync();
//await Sandbox.Providers.AmazonBedrock.RunAsync();
//await Sandbox.Providers.OpenAI.RunAsync();
//await AzureOpenAI.RunAsync();
//await Sandbox.Providers.Mistral.RunAsync();
//await Sandbox.Providers.Google.RunAsync();
//await Sandbox.Providers.Groq.RunAsync();
//await Sandbox.Providers.XAI.RunAsync();
//await Sandbox.Providers.OpenRouter.RunAsync();
//await Sandbox.Providers.Cerebras.RunAsync();

Console.WriteLine("Done");
return;


[AITool("my_tool")]
static string MyTool()
{
    return "42";
}
