using Microsoft.SemanticKernel.ChatCompletion;

namespace Markin.PromptValidator;

public class SessionContext
{
    public required string OriginalPrompt { get; init; }

    public ChatHistory ChatHistory { get; set; } = [];

    public string? ModifiedPrompt { get; set; }
}