using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text;
using WebSearchDemo;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var cred = new ApiKeyCredential(config["OpenAIAPIKey"]);

var handler = new CustomHttpMessageHandler();
handler.InnerHandler = new HttpClientHandler();

var httpClient = new HttpClient(handler);

var opts = new OpenAIClientOptions()
{
    Transport = new HttpClientPipelineTransport(httpClient)
};

var openAIClient = new OpenAIClient(cred, opts);

var client = openAIClient
    .GetResponsesClient()
    .AsIChatClient()
    .AsBuilder()
    .Build();

var options = new ChatOptions()
{
    Instructions = "You are an test agent used to demonstrate the Microsoft Multi Agent Framework.",
    Tools = new List<AITool>()
    {
        AIFunctionFactory.Create(Functions.VeryImportantAgentTool),
        new HostedWebSearchTool()
    },
    RawRepresentationFactory = client =>
    {
        return new CreateResponseOptions()
        {
            Model = "gpt-5.4",
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.High,
                ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
            },
            StoredOutputEnabled = false
        };
    },
    AllowMultipleToolCalls = false
};

var chatClientAgentOptions = new ChatClientAgentOptions()
{
    ChatOptions = options,
    ChatHistoryProvider = Functions.GetInMemoryChatHistoryProvider(client)
};

var agent = client.AsAIAgent(chatClientAgentOptions);

var session = await agent.CreateSessionAsync();

var message = "Use the web tool to get me the weather in Seattle and Brick, NJ.";

var responseObjectNames = new Dictionary<string, int>();

var contentTypes = new HashSet<string>();

try
{
    await foreach (var response in agent.RunStreamingAsync(message, session: session))
    {
        foreach (AIContent content in response.Contents)
        {
            switch (content)
            {
                case TextReasoningContent textReasoningContent:
                    Console.Write(textReasoningContent.Text);
                    break;

                case ErrorContent errorContent:
                    Console.WriteLine("ERROR!");
                    break;

                case FunctionCallContent functionCallContent:
                    Console.WriteLine();

                    var sb = new StringBuilder();
                    sb.Append($"Calling {functionCallContent.Name} function with arguments: ");

                    foreach (var kvp in functionCallContent.Arguments)
                    {
                        sb.Append($"{kvp.Key}={kvp.Value} ");
                    }
                    Console.WriteLine(sb.ToString());
                    break;

                case UsageContent usageContent:
                    break;

                default:
                    break;
            }
        }

        Console.Write(response.Text);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Exception thrown during agent execution: {ex}");
}


var responseString = CustomHttpMessageHandler.ResponseStringBuilder.ToString();

Console.WriteLine();

public class CustomHttpMessageHandler : DelegatingHandler
{
    public static StringBuilder ResponseStringBuilder = new StringBuilder();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        ResponseStringBuilder.Append(await response.Content.ReadAsStringAsync());
        return response;
    }
}



