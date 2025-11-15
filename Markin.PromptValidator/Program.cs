using Markin.PromptValidator;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;

var openAiApiKey = Environment.GetEnvironmentVariable("PROMPT_VALIDATOR__API_KEY", EnvironmentVariableTarget.User);
var openAiProxyUrl = Environment.GetEnvironmentVariable("PROMPT_VALIDATOR__OPENAI_PROXY_URL", EnvironmentVariableTarget.User) ?? "https://api.openai.com/v1";

if (string.IsNullOrWhiteSpace(openAiApiKey))
{
    Console.WriteLine("Не найден API Key для OpenAI. Установите ключ в переменную среды PROMPT_VALIDATOR__API_KEY");
    return;
}

var solutionDir = Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.Parent!.FullName;
var promptsFolder = Path.Combine(solutionDir, "testprompts");
var pathToPrompt = Path.Combine(promptsFolder, "Name Translator.txt");

var inputPromptText = await File.ReadAllTextAsync(pathToPrompt);

var userRequest = "Проанализируй промпт, найди все ошибки";

var services = new ServiceCollection();
services.AddKernel();

services.AddOpenAIChatCompletion("gpt-5-mini", new Uri(openAiProxyUrl), openAiApiKey);
services.AddLogging();

services.AddSingleton<PromptAnalyzer>();

services.AddSingleton(new SessionContext
{
    OriginalPrompt = inputPromptText
});

await using var servicesProvider = services.BuildServiceProvider();

var promptAnalyzer = servicesProvider.GetRequiredService<PromptAnalyzer>();
var sessionContext = servicesProvider.GetRequiredService<SessionContext>();

sessionContext.ChatHistory.AddUserMessage($"""
    Промпт для анализа:
    ```
    {sessionContext.ModifiedPrompt ?? sessionContext.OriginalPrompt}
    ```
    """);

sessionContext.ChatHistory.AddUserMessage(userRequest);

while (true)
{
    var response = await promptAnalyzer.AnalyzePrompt(sessionContext);
    sessionContext.ChatHistory.AddAssistantMessage(response);

    Console.WriteLine(response);

    Console.Write("> ");
    userRequest = Console.ReadLine();

    if (string.IsNullOrEmpty(userRequest))
        continue;
    else if (userRequest is "exit")
        break;

    sessionContext.ChatHistory.AddUserMessage(userRequest);
}