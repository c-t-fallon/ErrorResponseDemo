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

// gpt-5.2 -> none, low, medium, high, xhigh
// gpt-5.2-codex -> low, medium, high
// gpt-5-mini -> minimal, low, medium, high


var openAIClient = new OpenAIClient(cred, opts);

var client = openAIClient
    .GetResponsesClient("gpt-5-codex")
    .AsIChatClient()
    .AsBuilder()
    .ConfigureOptions(options =>
    {
        options.RawRepresentationFactory = _ => new CreateResponseOptions()
        {
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = "xhigh",
                ReasoningSummaryVerbosity = "auto"
            }
        };

        var additionalProperties = new AdditionalPropertiesDictionary();
        additionalProperties.Add("custom_property", 42);

        options.AdditionalProperties = additionalProperties;
    })
    .UseFunctionInvocation()
    .Build();

var options = new ChatOptions()
{
    Instructions = "You are an test agent used to demonstrate the Microsoft Multi Agent Framework.",
};

var message = "Hi";

var response = await client.GetResponseAsync(message, options);

var responseString = CustomHttpMessageHandler.ResponseStringBuilder.ToString();

Console.WriteLine();

public class CustomHttpMessageHandler : DelegatingHandler
{
    public static StringBuilder ResponseStringBuilder = new StringBuilder();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestConten = await request.Content.ReadAsStringAsync();

        var response = await base.SendAsync(request, cancellationToken);
        ResponseStringBuilder.Append(await response.Content.ReadAsStringAsync());
        return response;
    }
}