using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Markin.PromptValidator;

public class TodoPlugin(ILogger<TodoPlugin> logger)
{
    // [KernelFunction, Description("Запись ToDo-листа.")]
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

    // [KernelFunction, Description("Чтение ToDo-листа.")]
    public ICollection<AgenticTask> ReadTodo(
        [FromKernelServices] SessionContext sessionContext
        )
    {
        logger.LogInformation("Чтение Todo-листа.");
        return sessionContext.Tasks;
    }

    [KernelFunction, Description("Составление списка задач необходимых для выполнения запроса")]
    public async Task CreateTodo(
        [FromKernelServices] SessionContext sessionContext,
        [FromKernelServices] IChatCompletionService chatCompletionService,
        [Description("Запрос пользователя")]
        string userRequest
    )
    {
        var result = await chatCompletionService.GetChatMessageContentAsync($"""
            Ты — планировщик задач агентской системы работы с промптами. 
            Твоя единственная функция — разбивать запросы пользователей на последовательные, конкретные задачи для агентской системы.
            Ты должен составить план выполнения запроса пользователя, выполнив который, агентская система сможет точно выполнить запрос пользователя или ответить на его вопрос.

            Агентская система умеет:
            - Анализировать промпт на наличие ошибок, неточностей, дублирования или противоречия инструкций и прочих проблем.
            - Уточнять у пользователя необходимую информацию
            - Предлагать улучшения промпта

            ПРАВИЛА:
            - Составляй минимальный, но достаточный план (2-5 задач)
            - Каждая задача должна быть конкретным действием
            - Запрещены мета-задачи ("проанализировать", "понять", "составить план", "вернуть ответ пользователю")
            - Задачи должны выполняться строго последовательно
            

            Примеры хороших задач:
            - "Проанализировать промпт на предмет X"
            - "Найти проблемы в блоке Y" 
            - "Уточнить Z у пользователя"
            - "Подсчитать и классифицировать ошибки"

            Запрос пользователя:
            ```
            {userRequest}
            ```
            """, new OpenAIPromptExecutionSettings()
        {
            ReasoningEffort = new OpenAI.Chat.ChatReasoningEffortLevel("minimal"),
            ResponseFormat = typeof(ToDoList),
        });

        var todoList = JsonSerializer.Deserialize<ToDoList>(result.Content ?? "null");
        sessionContext.Tasks = todoList.Tasks.Select(x => new AgenticTask
        {
            Text = x,
            Status = AgenticTaskStatus.NotStarted
        }).ToArray() ?? [];

        var tasksText = string.Join("\n", sessionContext.Tasks.Select(x => x.ToString()));
        logger.LogInformation("Запись Todo-листа. Задачи: \n{Tasks}", tasksText);
    }
}
