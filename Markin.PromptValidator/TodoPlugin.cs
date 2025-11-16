using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Markin.PromptValidator;

public class TodoPlugin(ILogger<TodoPlugin> logger)
{
    [KernelFunction, Description("Запись ToDo-листа.")]
    public void WriteTodo(
        [FromKernelServices] SessionContext sessionContext,
        [Description("Список задач")]
        AgenticTask[] tasks
        )
    {
        var tasksText = string.Join("\n", tasks.Select(x => x.ToString()));
        logger.LogInformation("Запись Todo-листа. Задачи: {Tasks}", tasksText);

        sessionContext.Tasks = tasks;
    }
    
    [KernelFunction, Description("Чтение ToDo-листа.")]
    public ICollection<AgenticTask> ReadTodo(
        [FromKernelServices] SessionContext sessionContext
        )
    {
        logger.LogInformation("Чтение Todo-листа.");
        return sessionContext.Tasks;
    }
}