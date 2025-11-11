using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Markin.PromptValidator;

internal class DialogAgent(Kernel kernel, ILogger<DialogAgent> logger)
{
    private const string systemPrompt = """
    Ты - ассистент по работе с промптами.
    """;

    private static readonly OpenAIPromptExecutionSettings executionSettings = new()
    {
        ReasoningEffort = OpenAI.Chat.ChatReasoningEffortLevel.Low
    };

    public async Task<string> ProcessRequest(IEnumerable<ChatMessageContent> chatMessages)
    {
        logger.LogInformation("Обработка запроса пользователя...");

        ChatHistory chatHistory = [new ChatMessageContent(AuthorRole.System, systemPrompt), .. chatMessages];
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatCompletion.GetChatMessageContentAsync(chatHistory, executionSettings, kernel);

        return result.Content?.ToString() ?? "null";
    }
}
