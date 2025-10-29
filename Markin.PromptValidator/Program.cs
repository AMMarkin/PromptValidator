using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

var openAiApiKey = Environment.GetEnvironmentVariable("PROMPT_VALIDATOR__API_KEY", EnvironmentVariableTarget.User);

if(string.IsNullOrWhiteSpace(openAiApiKey))
{
    Console.WriteLine("Не найден API Key для OpenAI. Установите ключ в переменную среды PROMPT_VALIDATOR__API_KEY");
    return;
}

var solutionDir = Directory.GetParent(Environment.CurrentDirectory)!.Parent!.Parent!.Parent!.FullName;
var promptsFolder = Path.Combine(solutionDir, "testprompts");
var pathToPrompt = Path.Combine(promptsFolder, "Name Translator.txt");

Console.WriteLine($"Читаем промпт из файла: {pathToPrompt}");

var prompt = File.ReadAllText(pathToPrompt);

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-5-mini", openAiApiKey)
    .Build();

var logicAgent = new ChatCompletionAgent()
{
    Name = "Logic Agent",
    Description = "Агент проверяющий общую логику промпта",
    Kernel = kernel,
    Instructions = """
        Проверь промпт на наличие противоречий, опиши в тезисном формате со ссылками на изначальный текст
        """,
};

var startMessage = new ChatMessageContent(AuthorRole.User, prompt);

Console.WriteLine("Результат анализа:");

await foreach (var response in logicAgent.InvokeAsync(startMessage))
{
    Console.WriteLine(response.Message);
}