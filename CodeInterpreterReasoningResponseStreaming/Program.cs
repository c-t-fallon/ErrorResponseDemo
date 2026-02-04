#pragma warning disable OPENAI001

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
using System.Text;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var cred = new ApiKeyCredential(config["OpenAIAPIKey"]);
var openAIClient = new OpenAIClient(cred);

var agent = openAIClient
    .GetResponsesClient("gpt-5")
    .AsIChatClient()
    .AsBuilder()
    .ConfigureOptions(options =>
    {
        options.Tools = [new HostedCodeInterpreterTool()];
        options.RawRepresentationFactory = _ => new CreateResponseOptions()
        {
            ReasoningOptions = new ResponseReasoningOptions()
            {
                ReasoningEffortLevel = "medium",
                ReasoningSummaryVerbosity = "detailed"
            }
        };
    })
    .Build()
    .AsAIAgent();

var message = "Write and execute a simple, arbitrary python script with code interpreter.";

var reasoningCodeInterpreterResponse = new StringBuilder();
var reasoningContent = new StringBuilder();

Console.WriteLine("AIContent objects types received:");
Console.WriteLine("---------------------------------");

await foreach (var update in agent.RunStreamingAsync(message))
{
    switch (update.RawRepresentation)
    {
        case ChatResponseUpdate chatResponseUpdate:
            switch (chatResponseUpdate.RawRepresentation)
            {
                case StreamingResponseCodeInterpreterCallCodeDeltaUpdate delta:
                    reasoningCodeInterpreterResponse.Append(delta.Delta);
                    break;
                default:
                    break;
            }
            break;
    }

    foreach (var content in update.Contents)
    {
        Console.WriteLine(content.GetType().FullName);

        switch (content)
        {
            case TextReasoningContent textContent:
                reasoningContent.Append(textContent.Text);
                break;
        }
    }
}

Console.WriteLine();
Console.WriteLine("Code Interpreter Response:");
Console.WriteLine("--------------------------");
Console.WriteLine(reasoningCodeInterpreterResponse.ToString());
Console.WriteLine();
Console.WriteLine("Reasoning Response:");
Console.WriteLine("-------------------");
Console.WriteLine(reasoningContent.ToString());