#pragma warning disable OPENAI001

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
        AIFunctionFactory.Create(ToolWithOptionalArg),
        AIFunctionFactory.Create(ToolWithRequiredArg),
        AIFunctionFactory.Create(ToolWithWhatIThoughtShouldBeARequiredArg)
    }
};

var message = "Hi";

var response = await client.GetResponseAsync(message, options);

var responseString = CustomHttpMessageHandler.ResponseStringBuilder.ToString();

Console.WriteLine();

[DisplayName("tool_with_required_arg")]
[Description("A tool with an required argument.")]
static async Task<string> ToolWithRequiredArg([Required] int page)
{
    return "";
}

[DisplayName("tool_with_optional_arg")]
[Description("A tool with an optional argument.")]
static async Task<string> ToolWithOptionalArg(int page = 1)
{
    return "";
}

[DisplayName("tool_with_what_I_thought_should_be_required_arg")]
[Description("A tool with what I thought should be a required argument.")]
static async Task<string> ToolWithWhatIThoughtShouldBeARequiredArg([Required] int page = 1)
{
    return "";
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