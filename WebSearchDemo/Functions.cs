using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace WebSearchDemo;

internal static class Functions
{
    internal static ChatHistoryProvider GetInMemoryChatHistoryProvider(IChatClient chatClient)
    {
        var options = new InMemoryChatHistoryProviderOptions()
        {
            ChatReducer = new SummarizingChatReducer(chatClient, 4, null),
            ReducerTriggerEvent = InMemoryChatHistoryProviderOptions.ChatReducerTriggerEvent.BeforeMessagesRetrieval,
        };

        return new InMemoryChatHistoryProvider(options);
    }

    [DisplayName("very_important_agent_tool")]
    [Description("A very important agent tool")]
    internal static string VeryImportantAgentTool()
    {
        return "This was so important";
    }
}
