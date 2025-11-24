using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text;

#pragma warning disable OPENAI001

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
    .GetOpenAIResponseClient("gpt-5")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var options = new ChatOptions()
{
    Instructions = "You are an test agent used to demonstrate the Microsoft Multi Agent Framework.",
    Tools = new List<AITool>()
    {
        AIFunctionFactory.Create(GetBookPage)
    },
    RawRepresentationFactory = client =>
    {
        return new ResponseCreationOptions()
        {
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.High,
                ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
            }
        };
    }
};



var message = "Read the first 10 pages of the book and summarize it for me please.";

var responseObjectNames = new Dictionary<string, int>();

var contentTypes = new HashSet<string>();

await foreach (var response in client.GetStreamingResponseAsync(message, options))
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

var responseString = CustomHttpMessageHandler.ResponseStringBuilder.ToString();

Console.WriteLine();

[DisplayName("get_book_page")]
[Description("Gets the book page.")]
static async Task<string> GetBookPage(int page = 1)
{
    var contents = File.ReadAllText("book-war-and-peace.txt");
    return contents.Substring((page - 1) * 128_000, 128_000);
}

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