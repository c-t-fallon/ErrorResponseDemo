using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text;

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
    .GetResponsesClient("gpt-5.4-nano")
    .AsIChatClient()
    .AsBuilder()
    .Build();

var options = new ChatOptions()
{
    Instructions = "You are an test agent used to demonstrate the Microsoft Agent Framework.",
    Tools = new List<AITool>()
    {
        AIFunctionFactory.Create(GetBookPage)
    },
    AllowMultipleToolCalls = false,
    RawRepresentationFactory = client =>
    {
        return new CreateResponseOptions()
        {
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = "high",
                ReasoningSummaryVerbosity = "auto"
            },
            StoredOutputEnabled = true
        };
    }
};

var chatClientAgentOptions = new ChatClientAgentOptions()
{
    ChatOptions = options
};



var message = "Read the first 3 pages of the book and summarize it for me.";

var responseObjectNames = new Dictionary<string, int>();

var contentTypes = new HashSet<string>();

var updates = new List<AgentResponseUpdate>();

var agent = client.AsAIAgent(chatClientAgentOptions);

await foreach (var response in agent.RunStreamingAsync(message))
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

    updates.Add(response);
}

var responseString = CustomHttpMessageHandler.ResponseStringBuilder.ToString();

Console.WriteLine(responseString);

Console.WriteLine();

[DisplayName("get_book_page")]
[Description("Gets the book page.")]
static async Task<string> GetBookPage(int page = 1)
{
    var contents = File.ReadAllText("book-war-and-peace.txt");
    return contents.Substring((page - 1) * 128, 128);
}

public class CustomHttpMessageHandler : DelegatingHandler
{
    public static StringBuilder ResponseStringBuilder = new StringBuilder();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine(new string('-', 80));
        Console.WriteLine("Request:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine(await request.Content.ReadAsStringAsync());

        var response = await base.SendAsync(request, cancellationToken);

        Console.WriteLine(new string('-', 80));
        Console.WriteLine("Response:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine(await response.Content.ReadAsStringAsync());

        return response;
    }
}