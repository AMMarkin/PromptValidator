using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Markin.PromptValidator;

public class PromptAnalyzer(
    IChatCompletionService chatCompletion,
    Kernel kernel,
    SessionContext sessionContext)
{
    private const string promptAnalisysInstructions = """

        Ты - эксперт по анализу качества промптов для AI-систем. Твоя задача - находить расплывчатые, неконкретные формулировки в промптах пользователей.

        #### МЕТОДОЛОГИЯ АНАЛИЗА:

        Проведи анализ через последовательные этапы рассуждений:

        **Декомпозиция инструкций:**
        - Кратко выдели все явные инструкции 
        - Разбери промпт на составляющие элементы
        - Определи основные цели и подзадачи
        - Не переписывай исходный промпт

        **Анализ многозначности**
        Найди фразы, которые можно интерпретировать несколькими способами
        - "Какую интерпретацию выберет модель по умолчанию?"
        - "Будет ли эта интерпретация соответствовать реальным ожиданиям пользователя?"
        - "Есть ли более точные формулировки, которые исключат двусмысленность?"
        Фокус на практических несоответствиях, а не на гипотетических "глупостях" модели.
        Учти, что промпты обычно содержат плейсхолдеры для переменных. Не обращай на них внимания и не анализиуй их. Перед отправкой запроса на их место будет подставлено нужное значение, которое тебе недоступно.

        **Анализ формата ответа**
        - Если формат ответа не задан явно (JSON, XML, схема, структура), то анализ не требуется, проблем нет. Формат ответа (JSON/Plain text) обычно описывается в самом запросе к модели, а не в промпте. Некоторые модели сейчас вообще поддерживают response_format=json_schema, так что описывать структуру ответа в промпте может не требоваться. 
        - Если формат ответа явно задается (например, явно задается JSON схема, указаны требования к структуре данных), тогда обязательно требуется анализ соответствия инструкций этой структуре ответа.

        # Опорные критерии при анализе:

        ##### 1. КОНКРЕТНОСТЬ
        - Измеримость требований
        - Отсутствие субъективных оценок
        - Четкие параметры и критерии
        - Минимум интерпретаций
        - Ясные формулировки
        - Отсутствие двусмысленностей
        - Инструкции для исключительных ситуаций
        - Обработка некорректных входных данных

        ##### 2. КОНСИСТЕНТНОСТЬ
        - Отсутствие противоречий между инструкциями
        - Согласованность требований
        - Отсутствие взаимоисключающих условий

        ##### 3. ДЕЙСТВЕННОСТЬ
        - Четкий план выполнения
        - Определенные выходные форматы
        - Критерии успеха

        #### Требования к ответу:

        1. Указывай найденные замечания, обязательно со ссылками на исходный промпт.
        2. Не предлагай исправленный промпт и другие рекомендации - только анализ
        3. Поставь оценку ясности промпта от 1 до 10. Если промпт уже хороший - установи высокую оценку.
        
        #### Общие требования к анализу:

        1. Будь строгим, но конструктивным
        2. Фокусируйся на практических улучшениях
        3. Глубоко анализируй, но не будь дотошным - сфокусируйся на реальных проблемах в промпте. Лучше пара хороших замечаний, чем сто правок вида "пропущена запятая".
        4. Сналача анализ, потом вывод
        """;

    private const string systemPrompt = $"""
    Ты — консольный агент для работы с промптами, ориентированный на анализ, исправление и улучшение промптов.
    Работаешь интерактивно, ведёшь полноценный диалог с пользователем и активно участвуешь в разработке промпта.

    ---------------------------------------------------------------------
    ПЛАН ВЫПОЛНЕНИЯ 
    ---------------------------------------------------------------------
    
    Выполнение запроса пользователя, кроме уточняющего ответа на твой вопрос, начинается с составления плана выполнения. Вызови соответствующий инструмент, который составит план выполнения запроса и заверши свой ответ.
    Не начинай выполняй запрос пока не получишь указаний от системы о текущей задаче из составленного плана. 
    Выполняй эти задачи по одной. Если ты считаешь, что ты выполнил поставленную системой задачу из плана, то укажи в соответствующем поле ответа, что задача выполнена.
    Во время выполнения задачи от системы запрещено снова составлять план выполнения.

    ---------------------------------------------------------------------
    ОБЛАСТИ КОМПЕТЕНЦИЙ
    ---------------------------------------------------------------------

    ### 1. Анализ промпта
    ===================
    {promptAnalisysInstructions}
    ===================

    ### 2. Исправление ошибок
    Предлагай исправления согласно найденным ошибкам.

    ### 3. Улучшение промптов
    Предлагай улучшения проптов.

    Ответь в формате JSON.
    """;

    private static readonly OpenAIPromptExecutionSettings executionSettings = new()
    {
        ReasoningEffort = new OpenAI.Chat.ChatReasoningEffortLevel("minimal"),
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        ResponseFormat = typeof(PromptAnalyzerReport),
    };

    public async Task AnalyzePrompt(string userRequest)
    {
        if (sessionContext.ChatHistory.Count == 0)
        {
            sessionContext.ChatHistory.AddSystemMessage(systemPrompt);
            sessionContext.ChatHistory.AddUserMessage($"""
            Промпт для анализа:
            ```
            {sessionContext.OriginalPrompt}
            ```
            """);
        }
        sessionContext.ChatHistory.AddUserMessage(userRequest);
        var agentReport = await InvokeAgent();
        sessionContext.ChatHistory.AddAssistantMessage(agentReport.Text);

        while (sessionContext.Tasks.Any(x => x.Status != AgenticTaskStatus.Completed))
        {
            var currentTask = sessionContext.Tasks.First(x => x.Status != AgenticTaskStatus.Completed);
            currentTask.Status = AgenticTaskStatus.InProgress;
            sessionContext.ChatHistory.AddSystemMessage("Текущая задача: " + currentTask.Text);

            WriteSystemMessageToConsole("Выполнение задачи: " + currentTask.Text);

            agentReport = await InvokeAgent();
            sessionContext.ChatHistory.AddAssistantMessage(agentReport.Text);
            if (agentReport.IsTaskCompleted)
                currentTask.Status = AgenticTaskStatus.Completed;

            Console.WriteLine(agentReport.Text);
        }

        sessionContext.ChatHistory.AddSystemMessage($"Текущая задача: Озвучить финальный ответ пользователю");

        WriteSystemMessageToConsole("Финальный ответ:");
        agentReport = await InvokeAgent();
        sessionContext.ChatHistory.AddAssistantMessage(agentReport.Text);
        Console.WriteLine(agentReport.Text);

        static void WriteSystemMessageToConsole(string message)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
    
    private async Task<PromptAnalyzerReport> InvokeAgent()
    {
        var result = await chatCompletion.GetChatMessageContentAsync(sessionContext.ChatHistory, executionSettings, kernel);
        return JsonSerializer.Deserialize<PromptAnalyzerReport>(result.Content.ToString());
    }
}
