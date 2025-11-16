using System.ComponentModel;

namespace Markin.PromptValidator;

[Description("Ответ агента анализа промпта")]
internal class PromptAnalyzerReport
{
    [Description("Текст ответа")]
    public required string Text { get; set; }

    [Description("Текущий шаг выполнен?")]
    public required bool IsTaskCompleted { get; set; }
}