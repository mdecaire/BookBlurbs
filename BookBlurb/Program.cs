
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

var apiKey = Environment.GetEnvironmentVariable("OpenAIAPIKey", EnvironmentVariableTarget.User);
var firstPagePath = @"C:\Users\MLund\source\repos\BookBlurb\BookBlurb\Book\FirstPage.txt";

Kernel kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-3.5-turbo-0125", apiKey)
    .Build();

// Create a new chat
IChatCompletionService ai = kernel.GetRequiredService<IChatCompletionService>();
ChatHistory chat = new("You are an AI assistant that generates book blurbs.");
chat.AddUserMessage(File.ReadAllText(firstPagePath));
StringBuilder builder = new();


var arguments = new KernelArguments();

// Q&A loop
while (true)
{
    Console.Write("Question: ");
    chat.AddUserMessage(Console.ReadLine()!);
    builder.Clear();

    await foreach (var message in ai.GetStreamingChatMessageContentsAsync(chat))
    {
        Console.Write(message);
        builder.Append(message.Content);
    }
    Console.WriteLine();
    chat.AddAssistantMessage(builder.ToString());

    Console.WriteLine();

}



//kernel.ImportPluginFromFunctions("DateTimeHelper", [
//    kernel.CreateFunctionFromMethod(()=>$"{DateTime.UtcNow:r}","Now","Gets the current date and time")
//    ]);

//KernelFunction qa = kernel.CreateFunctionFromPrompt("""
//    The current date and time is {{ datetimehelper.now }}.
//    {{ $input }}
//    """);