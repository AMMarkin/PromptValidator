using Markin.PromptValidator;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
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

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddOpenAIChatCompletion("gpt-5-mini", openAiApiKey);
kernelBuilder.Services.AddLogging();
var kernel = kernelBuilder.Build();

var prompt = File.ReadAllText(pathToPrompt);

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-5-mini", openAiApiKey)
    .Build();

var logicAgent = LogicAgent.Create(kernel);

var startMessage = new ChatMessageContent(AuthorRole.User, prompt);

Console.WriteLine("Результат анализа:");

await foreach (var response in logicAgent.InvokeAsync(startMessage))
{
    Console.WriteLine(response.Message);
}