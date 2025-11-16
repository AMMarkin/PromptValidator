using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Markin.PromptValidator;

public class SessionContext
{
    public required string OriginalPrompt { get; init; }

    public string? ModifiedPrompt { get; set; }

    public ChatHistory ChatHistory { get; set; } = [];

    public ICollection<AgenticTask> Tasks { get; set; } = [];
}

[Description("Задача агента")]
public class AgenticTask
{
    [Description("Текст задачи")]
    public required string Text { get; set; }

    [Description("Статус задачи")]
    public required AgenticTaskStatus Status { get; set; }

    public override string ToString() => $"Статус: '{Status}'. Текст: '{Text}'.";
};

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgenticTaskStatus
{
    NotStarted,
    InProgress,
    Completed
}