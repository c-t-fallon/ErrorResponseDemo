#pragma warning disable OPENAI001
#pragma warning disable MAAI001

using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using System.Text;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var serviceCollection = new ServiceCollection();

serviceCollection.AddLogging(configure =>
{
    configure.AddDebug().SetMinimumLevel(LogLevel.Trace);
});

var serviceProvider = serviceCollection.BuildServiceProvider();

var skillPath = Path.Combine(AppContext.BaseDirectory, "skills");
var skillsProvider = new FileAgentSkillsProvider(skillPath: skillPath);

var client = new Client(apiKey: config["GeminiAPIKey"]);


var chatClient = client
    .AsIChatClient("gemini-3-flash-preview")
    .AsBuilder()
    .ConfigureOptions(o =>
    {
        o.RawRepresentationFactory = _ => new GenerateContentConfig()
        {
            ThinkingConfig = new ThinkingConfig()
            {
                IncludeThoughts = true,
                ThinkingLevel = GetThinkingLevel("high")
            }
        };
    })
    .UseFunctionInvocation()
    .Build(serviceProvider);

ThinkingLevel GetThinkingLevel(string effortLevel) => effortLevel switch
{
    "minimal" => ThinkingLevel.Minimal,
    "low" => ThinkingLevel.Low,
    "medium" => ThinkingLevel.Medium,
    "high" => ThinkingLevel.High,
    _ => throw new NotSupportedException("Unsupported effort level for Gemini model."),
};

AIAgent agent = chatClient
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "SkillsAgent",
        ChatOptions = new()
        {
            Instructions = "You are a helpful assistant.",
        },
        AIContextProviders = [skillsProvider],
    });

// --- Example 2: Filing an expense report (multi-turn with template asset) ---
Console.WriteLine("Example 2: Filing an expense report");
Console.WriteLine("---------------------------------------");
AgentSession session = await agent.CreateSessionAsync();
AgentResponse response2 = await agent.RunAsync("I had 3 client dinners and a $1,200 flight last week. Return a draft expense report and ask about any missing details.",
    session);
Console.WriteLine($"Agent: {response2.Text}\n");

var response3 = await agent.RunAsync("Are you sure thats right?", session);


Console.WriteLine("---");

Console.WriteLine($"Agent: {response3.Text}\n");

public class CustomHttpMessageHandler : DelegatingHandler
{
    public static StringBuilder ResponseStringBuilder = new StringBuilder();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestString = await request.Content.ReadAsStringAsync();

        var response = await base.SendAsync(request, cancellationToken);
        ResponseStringBuilder.Append(await response.Content.ReadAsStringAsync());
        return response;
    }
}