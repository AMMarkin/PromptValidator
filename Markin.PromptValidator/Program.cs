using Markin.PromptValidator;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;

var openAiApiKey = Environment.GetEnvironmentVariable("PROMPT_VALIDATOR__API_KEY", EnvironmentVariableTarget.User);

if(string.IsNullOrWhiteSpace(openAiApiKey))
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

services.AddOpenAIChatCompletion("gpt-5-mini", openAiApiKey);
services.AddLogging();

services.AddSingleton<LogicAgent>();
services.AddSingleton<DialogAgent>();

services.AddSingleton(new SessionContext
{
    OriginalPrompt = inputPromptText
});

await using var servicesProvider = services.BuildServiceProvider();

var dialogAgent = servicesProvider.GetRequiredService<DialogAgent>();
var sessionContext = servicesProvider.GetRequiredService<SessionContext>();

sessionContext.ChatHistory.AddUserMessage(userRequest);

while (true)
{
    var response = await dialogAgent.ProcessRequest(sessionContext.ChatHistory);
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