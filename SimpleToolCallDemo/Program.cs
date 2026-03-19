using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
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
    .GetResponsesClient("gpt-5")
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

var options = new ChatOptions()
{
    Instructions = "You are an test agent used to demonstrate the Microsoft Multi Agent Framework.",
    Tools = new List<AITool>()
    {
        AIFunctionFactory.Create(AgentTool)
    },

    RawRepresentationFactory = client =>
    {
        return new CreateResponseOptions()
        {
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.High,
                ReasoningSummaryVerbosity = ResponseReasoningSummaryVerbosity.Detailed
            }
        };
    }
};

var result = await client.GetResponseAsync("hello", options);

Console.WriteLine();

[DisplayName("agent_tool")]
[Description("Agent tool")]
static ToolResult AgentTool()
{
    return null;
}

public class CustomHttpMessageHandler : DelegatingHandler
{
    public static StringBuilder ResponseStringBuilder = new StringBuilder();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine("Request:");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(await request.Content.ReadAsStringAsync());

        var response = await base.SendAsync(request, cancellationToken);

        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine("Response:");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(await response.Content.ReadAsStringAsync());

        return response;
    }
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