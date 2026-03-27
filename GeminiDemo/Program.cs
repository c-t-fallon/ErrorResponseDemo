#pragma warning disable OPENAI001
#pragma warning disable MAAI001

using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var client = new Client(apiKey: config["GeminiAPIKey"])
    .AsIChatClient("gemini-3-flash-preview")
    .AsBuilder()
    .ConfigureOptions(options =>
    {
        options.Instructions = "You are an test agent used to demonstrate the Microsoft Multi Agent Framework.";
        options.Tools = new List<AITool>()
        {
            AIFunctionFactory.Create(AgentTool)
        };
        options.RawRepresentationFactory = _ => new GenerateContentConfig()
        {
            ThinkingConfig = new ThinkingConfig()
            {
                IncludeThoughts = true,
                ThinkingLevel = GetThinkingLevel("minimal")
            }
        };
    })
    .UseFunctionInvocation()
    .Build();

client.AsAIAgent(new ChatClientAgentOptions()
{
    AIContextProviders = new List<AIContextProvider>()
    {
        new FileAgentSkillsProvider()
    }
});

ThinkingLevel GetThinkingLevel(string effortLevel) => effortLevel switch
{
    "minimal" => ThinkingLevel.Minimal,
    "low" => ThinkingLevel.Low,
    "medium" => ThinkingLevel.Medium,
    "high" => ThinkingLevel.High,
    _ => throw new NotSupportedException("Unsupported effort level for Gemini model."),
};

var result = await client.GetResponseAsync("hello");

Console.WriteLine(result.Text);

[DisplayName("agent_tool")]
[Description("Agent tool")]
static ToolResult AgentTool()
{
    return new ToolResult() { StringProperty = "this is an agent tool" };
}

public class ToolResult
{
    public int IntegerProperty { get; set; }

    public double DoubleProperty { get; set; }

    public string StringProperty { get; set; }

    public ComplexProperty ComplexProperty { get; set; }
}

public class ComplexProperty
{
    public string Name { get; set; }

    public int Age { get; set; }
}