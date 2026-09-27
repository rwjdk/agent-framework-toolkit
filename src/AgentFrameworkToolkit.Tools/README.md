# Agent Framework Toolkit @ Tools

> This package is aimed at making it easier to consume AI Tools in [Microsoft Agent Framework](https://github.com/microsoft/agent-framework)

Check out the [General README.md](https://github.com/rwjdk/agent-framework-toolkit/blob/main/README.md) for Agentfactory providers and other shared features in Agent Framework Toolkit.

When using `ConfinedToTheseDomains` with `HttpClientTools` or `WebsiteTools`, the toolkit creates an HTTP client that checks each redirect destination. `HttpClientFactory` cannot be combined with domain confinement because a supplied client may follow redirects before the toolkit can check them.

## Samples
```cs
//1. Make your tool-class and add [AITool] attributes

public class MyTools
{
    [AITool]
    public string MyTool1()
    {
        return "hello";
    }

    [AITool]
    public string MyTool2()
    {
        return "world";
    }
}

//2. Get your tool by either instance or Type (if no constructor dependencies)

IList<AITool> tools = aiToolsFactory.GetTools(typeof(MyTools));
//or
IList<AITool> tools = aiToolsFactory.GetTools(new MyTools());
```
