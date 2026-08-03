# Changelog - Agent Framework Toolkit

## Version 1.16.1 (3rd of August 2026)
- Fixed request-specific state leaking between sequential tool-call rounds by returning fresh provider request options for OpenAI, Anthropic, and Google agents

---

## Version 1.16.0 (30th of July 2026)
- Updated Agent Framework from 1.15.0 to 1.16.0
- Updated all NuGet packages to the latest
- Updated the ModelContextProtocol NuGet package from 1.4.1 to 2.0.0

---

## Version 1.15.0 (23rd of July 2026)
- Updated Agent Framework from 1.14.0 to 1.15.0
- Updated all NuGet packages to the latest

---

## Version 1.14.0 (21st of July 2026)
- Updated Agent Framework from 1.13.0 to 1.14.0
- Updated all NuGet packages to the latest
- Added Microsoft Foundry provider (`AgentFrameworkToolkit.MicrosoftFoundry`) (In-Memory and Declarative Agents)
- Added Groq (www.groq.com) provider (`AgentFrameworkToolkit.Groq`)
- Removed AgentFrameworkToolkit.GitHub as the [GitHub Models Service is being retired](https://github.blog/changelog/2026-07-01-github-models-is-being-fully-retired-on-july-30-2026)
- Added known model-names for OpenAI (5.6 Sol, 5.6 Terra, 5.6 Luna)
- Added known model-names for Google (Gemini 3.5 Flash)
- Added known model-names for xAI (Grok 4.5), 
- Added known model-names for Anthropic (Sonnet 5) and OpenRouter (Various)
- Updated icon to the new MAF logo

---

## Version 1.13.0 (3rd of July 2026)
- Updated Agent Framework from 1.12.0 to 1.13.0
- Updated all NuGet packages to the latest

---

## Version 1.12.0 (1st of July 2026)
- Updated Agent Framework from 1.11.1 to 1.12.0
- Updated all NuGet packages to the latest

---

## Version 1.11.1 (26th of June 2026)
- Updated Agent Framework from 1.11.0 to 1.11.1
- Updated all NuGet packages to the latest

---

## Version 1.11.0 (23rd of June 2026)
- Updated Agent Framework from 1.10.0 to 1.11.0
- Updated all NuGet packages to the latest

---

## Version 1.10.0 (10th of June 2026)
- Updated Agent Framework from 1.9.0 to 1.10.0
- Updated all NuGet packages to the latest
- Anthropic: Added support for `Effort` and `ServiceTier` parameters
- Anthropic: Added Opus 4.7, 4.8 and Fable 5 as known models
- Google: Added support for `ServiceTier` parameters

---

## Version 1.9.0 (4th of June 2026)
- Updated Agent Framework from 1.8.0 to 1.9.0
- Updated all NuGet packages to the latest

---

## Version 1.8.0 (29th of May 2026)
- Updated Agent Framework from 1.7.0 to 1.8.0 [NOTE: Google is working again]
- Updated all NuGet packages to the latest

---

## Version 1.7.0 (27th of May 2026)
- Updated Agent Framework from 1.6.1 to 1.7.0 [NOTE: Due to dependency incompatibility, there is no package in this release that targets Google; it will return once Google updates their dependencies]
- Updated all NuGet packages to the latest

---

## Version 1.6.2 (20th of May 2026)
- Google: Added support for inspecting HttpRawCall Details
- OpenAI: Added support for the `store` = true/false setting via `StoredOutputEnabled` [#10](https://github.com/rwjdk/AgentFrameworkToolkit/issues/10)
- Anthropic: Added support for `CacheControlTimeToLive` to control caching strategy. [#62](https://github.com/rwjdk/AgentFrameworkToolkit/issues/62)

---

## Version 1.6.1 (14th of May 2026)
- Updated Agent Framework from 1.5.0 to 1.6.1 [NOTE: Due to dependency incompatibility, there is no package in this release that targets Google; it will return once Google updates their dependencies]
- Updated all NuGet packages to the latest

---

## Version 1.5.0 (14th of May 2026)
- Updated Agent Framework from 1.4.0 to 1.5.0 [NOTE: Due to dependency incompatibility, there is no package in this release that targets Google; it will return once Google updates their dependencies]
- Updated all NuGet packages to the latest

---

## Version 1.4.0 (5th of May 2026)
- Updated Agent Framework from 1.3.0 to 1.4.0 
- Fix that AgentSkills License/Compatibility/AllowedTools information was not correctly included ([PR#64](https://github.com/rwjdk/AgentFrameworkToolkit/pull/64) - Thank you to [UrienZhang](https://github.com/UrienZhang))
- Made Tools, Tools.ModelContextProtocol and AgentSkills dependent on Microsoft.Agents.AI instead of Microsoft.Extensions.AI.Abstractions as latter package tend to be ahead of AF and cause incompatible states.
- Updated all NuGet packages to the latest

---

## Version 1.3.0 (24th of April 2026)
- Updated Agent Framework from 1.2.0 to 1.3.0 
- Updated all NuGet packages to the latest

---

## Version 1.2.0 (22nd of April 2026)
- Updated Agent Framework from 1.1.0 to 1.2.0 
- Updated all NuGet packages to the latest

---

## Version 1.1.0 (10th of April 2026)
- Updated Agent Framework from 1.0.0 to 1.1.0
- Updated AWSSDK NuGets to the latest
- Updated OpenTelemetry NuGet to the latest
- Anthropic: Added `UseAdaptiveThinking` to use with 4.6 Models instead of the `BudgetTokens` property

---

## Version 1.0.0 (2nd of April 2026)
- Updated Agent Framework from rc5 to 1.0.0 (General Availability)

---

## Version 1.0.0-rc5 (31st of March 2026)
- Updated Agent Framework from rc4 to rc5

---

## Version 1.0.0-rc4.2 (31st of March 2026)
- Fixed that `ChatBatchRunResult` (intended name) was incorrectly called `BatchRunResult`

---

## Version 1.0.0-rc4.1 (31st of March 2026)
- Added `AzureOpenAIBatchRunner` for Chat Batch Capabilities
- Added `OpenAIBatchRunner` for Chat and Embedding Batch Capabilities
- Updated AWSSDK NuGets to the latest
- Updated Azure.Identity NuGet to the latest
- Updated OpenTelemetry NuGet to the latest
- Added GPT 5.4 mini/nano as known models

---

## Version 1.0.0-rc4 (12th of March 2026)
- Updated Agent Framework from rc3 to rc4
- Updated ModelContextProtocol from 1.0.0 to 1.1.0.
- Updated AWSSDK.BedrockRuntime from 4.0.16.1 to 4.0.16.2
- Updated AWSSDK.Extensions.Bedrock.MEAI from 4.0.5.8 to 4.0.5.9
- Updated Azure.Identity from 1.18.0 to 1.19.0
- Updated Google.GenAI from 1.2.0 to 1.3.0

---
 
## Version 1.0.0-rc3.1 (8th of March 2026)
- Anthropic: Added Support for Structured Output (custom implementation, as Anthropic does not adhere to common practice, but the outcome is the same).
- Update the various Model Constants with the latest releases
- AzureOpenAI: Endpoint Pattern `https://<name>.openai.azure.com/openai/v1` is now also auto-corrected by default (to: `https://<name>.openai.azure.com`)
- Added `AgentSession.GetMessages()` extension method for easier discoverability on getting messages from a session (Require that the session belongs to an agent that uses the default InMemoryChatHistory)
- Added Cerebras provider (`AgentFrameworkToolkit.Cerebras`)

---

## Version 1.0.0-rc3 (4th of March 2026)
- Bump Agent Framework from rc2 to rc3
- Bump Google.GenAI NuGet from 1.1.0 to 1.2.0
- Bump Azure.Identity from 1.17.1 to 1.18.0

---

## Version 1.0.0-rc2 (26th of Feb 2026)
- Bump Microsoft Agent Framework from v.1.0-rc1 to v.1.0-rc2
- Removed `.RunAsync<T>(...)` polyfill (AIAgentExtensions.cs) that dealt with [AF Issue #4118](https://github.com/microsoft/agent-framework/issues/4118) as it is now fixed.
- Added dedicated Constructors for the various connection classes for easier construction.
- Bump ModelContextProtocol NuGet from 0.8.0-preview1 to 1.0.0
- Bump AWSSDK.BedrockRuntime" NuGet from 4.0.16 to 4.0.16.1
- Bump AWSSDK.Extensions.Bedrock.MEAI Nuget from 4.0.5.7 to 4.0.5.8

---

## Version 1.0.0-rc1 (21st of Feb 2026)
- [BREAKING] Upgraded and fixed breaking changes from Microsoft Agent Framework v.1.0-rc1
  - Important Note: AF now has `.RunAsync<T>(...)` directly on AIAgent, which, on paper, makes `AIAgentExtensions.cs` not needed anymore, but due to a bug in RC1 (https://github.com/microsoft/agent-framework/issues/4118) it is currently used as a polyfill for Microsoft's bug. This however, has a side-effect that if you use the AgentFactories, but consume the agents as `AIAgent` I can't serve the polyfill version to you due to new real (buggy) implementation and polyfill have same name. Workaround for this is to consume factor as named Agent (example `OpenAIAgent` or my generic version `Agent` or use the extension method like this `AIAgentExtensions.RunAsync<T>()`). Once Microsoft fix their issue the polyfill workaround can go away and all scenarions will work again
- AgentSkills: Fixed the body of the skill did not add line breaks ([PR#46](https://github.com/rwjdk/AgentFrameworkToolkit/pull/46) : Thanks to [@visasnouski](https://github.com/) for the fix 👍)

---

## Version 1.0.0-preview.260216.1
- [BREAKING] Upgraded and fixed breaking changes from Microsoft Agent Framework v.1.0.0-preview.260212.1

---

## Version 1.0.0-preview.260212.1
- Added `RandomTools` (Suggest random number since AI is not good at that)
- Added `EmailTools` (Send basic SMTP Emails via AI)
- OpenAI: Added option to set `service_tier`
- Upgraded Google.GenAI NuGet from 0.15.0 to 1.0.0

---

## Version 1.0.0-preview.260209.1
- [BREAKING] Upgraded and fixed breaking changes from Microsoft Agent Framework v.1.0.0-preview.260209.1
- Added domain confinement options for `HttpClientTools` and `WebsiteTools` to allow restricting calls to exact allowed domains.

---

## Version 1.0.0-preview.260206.1
- [BREAKING] Upgraded and fixed breaking changes from Microsoft Agent Framework v.1.0.0-preview.260205.1
  - `GetNewSessionAsync` is now called ``
- Bump Google.GenAI to 0.15.0
- Bump AWSSDK.BedrockRuntime to 4.0.16
- Bump ModelContextProtocol to 0.8.0-preview.1
- All Provider-specific Agents now derive from a generic `Agent` (with the raw AIAgent as the Inner Agent) to reduce code redundency

---

## Version 1.0.0-preview.260203.1
- Bump Google.GenAI from 0.13.1 to 0.14.0 (PR#42)
- Bump AWSSDK.BedrockRuntime and AWSSDK.Extensions.Bedrock.MEAI (PR#41)

---

## Version 1.0.0-preview.260128.1
- Upgraded to Microsoft Agent Framework v.1.0.0-preview.260128.1 (No changes)

---

## Version 1.0.0-preview.260127.1
- [BREAKING] Upgraded and fixed breaking changes from Microsoft Agent Framework v.1.0.0-preview.260127.1 
  - `ChatHistoryProviderFactory` is now called `ChatHistoryProviderFactory`

---

## Version 1.0.0-preview.260126.1
- Added Amazon Bedrock provider (`AgentFrameworkToolkit.AmazonBedrock`)
- Moved AIToolsFactory to its own NuGetPackage (`AgentFrameworkToolkit.Tool` to allow usage without dependency on `Microsoft.Agents.AI`)
- Added a common set of AI Tools ('FileSystemTools', 'HttpClientTools', 'Timetools', 'WeatherTools', and 'WebsiteTools')
- Google: Added `Networktimeout` option (so you don't need to define it in miliseconds via http-options)
- Google: Bump internal NuGet to 0.13.1

---

## Version 1.0.0-preview.260121.1
- [BREAKING] Upgraded and fixed breaking changes from Microsoft Agent Framework v.1.0.0-preview.260121.1
- Bumped OpenTelemetry package to 1.15.0

---

## Version 1.0.0-preview.260120.1
- Added `GetTextReasoningContent` extension method to easily get the Reasoning text (if any) from an AgentResponse.
- Added `GetContainerFileCitationMessageAnnotations` extension method to easily get ContainerFileCitations from, for example, a Code Interpreter usage (OpenAI-based agents only).
- Google: Bump internal NuGet to 0.12.0

---

## Version 1.0.0-preview.260115.1
- Google: [BREAKING] Switched to the official Google NuGet Package (only breaking in Connection part if Google Vertex is used)
- Google: Added GoogleEmbeddingFactory
- Google: Added`ThinkingLevel` and `IncludeThoughts` in options
- `.RunAsync<T>(...)` in AIAgentExtensions now used provided serializationOptions (if given); only creating it's own if null.
- `.RunAsync<T>(...)` now support List/Array as T (using similar object wrapping technique as Microsoft.Extensions.AI)
- AzureOpenAI: Added Auto-correction of the Endpoint if you by mistake give the entire foundry url 'https://[name].services.ai.azure.com/api/projects/[project]' by on the fly correcting it to 'https://[name].services.ai.azure.com'. This is on by default, but can be turned off by setting property `AutoCorrectFoundryEndpoint` to false
- Fixed that the AIAgentExtensions was in a wrongly named class called AIToolsExtensions
- [BREAKING] `ApplyMiddleware` on the various options objects are moved to a central `MiddlewareHelper` to reduce code-duplication

---

## Version 1.0.0-preview.260108.1
- Bumped Agent Framework Nuget Packages to '1.0.0-preview.260108.1' and fixed breaking changes (nothing breaking for the Toolkit itself)

---

## Version 1.0.0-preview.260104.1
- Added option to work with [AgentSkills](https://agentskills.io) in the `AIToolsFactory` or with the dedicated `AgentSkillsFactory`
- Anthropic: Added `Endpoint` property to connection so you can override the default BaseUrl (and, for example, consume an Anthropic model from Microsoft Foundry using the Anthropic API)

---

## Version 1.0.0-preview.251230.1
- Added `OpenAIEmbeddingModels` const-collection of Open AI Embedding Models
- Exposed `Connection` as a Property on all Factories
- NuGet: Changed Project URL to be the [Wiki](https://github.com/rwjdk/AgentFrameworkToolkit/wiki)
- Upgraded 'Google_GenerativeAI.Microsoft' NuGet from 3.4.1 to 3.6.1

---

## Version 1.0.0-preview.251226.1
- Added support for setting `AIContextProviderFactory` and `ChatHistoryProviderFactory` on Agents
- Removed obsolete `OpenAIAgentOptionsForChatClientWithoutReasoning`, `OpenAIAgentOptionsForChatClientWithReasoning`, `OpenAIAgentOptionsForResponseApiWithoutReasoning` and `OpenAIAgentOptionsForResponseApiWithReasoning` (Use `AgentOptions` instead)
- Added missing Dependency Injection options for EmbeddingFactory in Providers `Mistral` and `OpenRouter`
- Added Cohere as provider
- [BREAKING] Athropic: Renamed 'maxTokenCount' to 'maxOutputTokens' to have consistent naming in the simplified CreateAgent method

---

## Version 1.0.0-preview.251224.1
- Added support for easy Tool-Calling Middleware (allowing you to inspect, manipulate and cancel tool-calling)
- Added support for easy OpenTelemetry Middleware (Logging in the OpenTelemetry standard)
- Added support for easy Logging Middleware (Custom Logging)
- Added OpenRouterEmbeddingFactory
- Added MistralEmbeddingFactory
- Restructured README
- Added individual provider README files

---

## Version 1.0.0-preview.251222.1
- Changed `AITool[]?` parameter in simplified `CreateAgent` methods to be `IList<AITool>?` for consistency
- Fixed that the OpenAI based simplified  `CreateAgent` methods (aka not using AgentOptions) forced `ClientType.ChatClient` instead of using the default in the Connection.
- Fixed that if ClientType was not set on Agent Level, Reasoning Effort was not properly added
- Fixed that MCP Tools could be defined but not called
- [BREAKING] GitHub: Renamed `PersonalAccessToken` or `AccessToken` as it do not need to be personal all the time (it can example be generated by a GitHub App)
- Added Unit/Integration-tests to try and avoid silly mistakes like those in previous and this release in going forward.

---

## Version 1.0.0-preview.251220.1
- Fixed that not all OpenAI based `CreateAgent` methods would set the `LoggerFactory`, `ClientFactory` and `Services` (Thank you to [LennartJohansen](https://github.com/LennartJohansen) for pointing it out in [#26](https://github.com/rwjdk/AgentFrameworkToolkit/issues/26)

---

## Version 1.0.0-preview.251219.1
- Added central build management and reorganized project structure (Huge thanks to [gurolg](https://github.com/gurolg) for [PR#24](https://github.com/rwjdk/AgentFrameworkToolkit/pull/24) doing the work)
- Add [CONTRIBUTING.md](https://github.com/rwjdk/AgentFrameworkToolkit/blob/main/CONTRIBUTING.md) (Again thanks to [gurolg](https://github.com/gurolg))
- Bumped `Microsoft.Agents.AI` version to latest (1.0.0-preview.251219.1) to be compatible with [latest breaking changes](https://github.com/microsoft/agent-framework/releases/tag/dotnet-1.0.0-preview.251219.1)
  - Removed Agent Display name as AF does not have it anymore
  - Remove Agent ID as an override, as it is not overridable anymore 
  - Resolved new OpenAI Nuget Package renames in ResponsesAPI
- Renamed `ToolCallsHandler.ToolCallingMiddleware` to `ToolCallingMiddlewareAsync`
- Added `DefaultClientType` to OpenAI/AzureOpenAI-based providers so you can define if `ChatClient` or `ResponsesAPI` is the default `ClientType` for Agents that do not define it themself

---

## Version 1.0.0-preview.251217.1
- Added `AIToolsFactory` to make it easier to create and define AI Tools
- [BREAKING] Obsoleted `OpenAIAgentOptionsForChatClientWithoutReasoning`, `OpenAIAgentOptionsForChatClientWithReasoning`, `OpenAIAgentOptionsForResponseApiWithoutReasoning` and `OpenAIAgentOptionsForResponseApiWithReasoning` as these names were 'too much' and confused (Use AgentOptions instead (with `ClientType` and `ReasoningEffort` Enums to control the option))
- Added support for `IServiceProvider`, `ILoggerFactory`, and `ClientFactory` in Agent Creation

---

## Version 1.0.0-preview.251215.1
- Added option to get Raw Client from the various Connection Objects (Except for the Google Connection, as switch to the new official Google Nuget is expected soon)
- OpenRouter and XAI Connection are now inherited from OpenAI Connections
- OpenAI + AzureOpenAI: Added EmbeddingFactory

---

## Version 1.0.0-preview.251204.1
- Bumped `Microsoft.Agents.AI` version to latest (1.0.0-preview.251204.1) to be compatible with [latest breaking change around Instructions](https://github.com/microsoft/agent-framework/pull/1517)

---

## Version 1.0.0-preview.251201.1
- AzureOpenAI: Added RBAC Support (TokenCredentials)
- Enabled "Treat Warnings as Errors"
- Added .editorconfig 
- Removed all todos in the code.

---

## Version 1.0.0-preview.251129.1
- Added GitHub provider NuGet Package
- Everything now has XML Summaries
- [BREAKING] Renamed `DeploymentModelName` to `Model` to make it simpler to understand (Sorry to existing users, but better now than later)
- [BREAKING] Renamed `RequestJson` and `ResponseJson` to `RequestData` and `ResponseData` as not all LLMs use JSON for communication (e.g., Anthropic response data is not JSON)
- OpenAI: Added `WithOpenAIResponsesApiReasoning` and `WithOpenAIChatClientReasoning` Extension Methods for `ChatOptions` (if you do not wish to use AgentFactory, but still wish to have an easier time to set OpenAI Reasoning)
- Fixed that `ServiceCollectionExtensions` for Google was in the wrong namespace
- Fixed that Mistral had an `AddAnthropicAgentFactory` method (wrong name)
- Fixed that OpenAI ResponseAPI without reasoning, Agents did not get their Temperature set

---

## Version 1.0.0-preview.251126.2
- Anthropic: Replaced the unofficial `Anthropic.SDK` nuget package with the official `Microsoft.Agents.AI.Anthropic` nuget package instead.
- [BREAKING] Removed the `UseInterleavedThinking` option from the Anthropic Package, as it actually did not do anything.

---

## Version 1.0.0-preview.251126.1
- Bumped `Microsoft.Agents.AI` version to latest (1.0.0-preview.251125.1)
- Fixed that RawCallDetail would fail if input or output was not JSON (like the output of an Anthropic call)

---

## Version 1.0.0-preview.251123.0
- Added OpenRouter provider NuGet Package
- [OpenAI] Added `OpenAIChatModels` of the most common models
- [Anthropic] Added `AnthropicChatModels` of the most common models
- [XAI] Added `XAIChatModels` of the most common models
- [Mistral] Added `MistralChatModels` of the most common models
- [Google] Added `GoogleChatModels` of the most common models
- [OpenRouter] Added `OpenRouterChatModels` of the most common models

---

## Version 1.0.0-preview.251121.3
- [BREAKING] Moved NetworkTimeout from Request to Connection as it makes more sense (might introduce a per-agent override if needed in the future)
- Added Dependency Injection Methods for all the AgentFactories
- Added Extension Method for AIAgent to have .RunAsync<>(...) for Structured Output

---

## Version 1.0.0-preview.251121.2
- Added Mistral provider NuGet Package
- Gave each package its own Description

---

## Version 1.0.0-preview.251121.1
- Added Samples to README.md
- Moved to central NuGet-props + adopted the same versioning as Microsoft Agent Framework
- Added Simplified Agent Factory Constructors
- Added various simplified CreateAgent (XAI, OpenAI, AzureOpenAI)

---
