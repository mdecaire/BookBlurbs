using BookBlurb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Sqlite;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;
using System.Text;
using System.Text.RegularExpressions;


#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010,SKEXP0011,SKEXP0050,SKEXP0052, SKEXP0055, SKEXP0020 // Experimental

var apiKey = Environment.GetEnvironmentVariable("OpenAIAPIKey", EnvironmentVariableTarget.User);
var filePath = @"C:\Users\MLund\source\repos\BookBlurb\BookBlurb\Book\rm.docx";
    //@"C:\Users\MLund\source\repos\BookBlurb\BookBlurb\Book\FirstPage.txt";
    //@"C:\Users\MLund\source\repos\BookBlurb\BookBlurb\Book\rm.docx";

IKernelBuilder kb = 
    Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-3.5-turbo-0125", apiKey);
kb.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());

Kernel kernel = kb.Build();

kb.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information); // Only show Information level or higher logs
});

// Download a document and create embeddings for it
ISemanticTextMemory memory = new MemoryBuilder()
    .WithLoggerFactory(kernel.LoggerFactory)
    .WithMemoryStore(await SqliteMemoryStore.ConnectAsync("mydata.db"))
    .WithOpenAITextEmbeddingGeneration("text-embedding-3-small", apiKey)
    .Build();

IList<string> collections = await memory.GetCollectionsAsync();

var collectionName = "BookBlurb";
if (collections.Contains(collectionName))
{
    Console.WriteLine("Found database");
}
else
{
    var paragraphs = BookHelpers.ConvertToPlainText(filePath);
  
    
    var tokenList =
    TextChunker.SplitPlainTextParagraphs(
            TextChunker.SplitPlainTextLines(
               Regex.Replace(paragraphs, @"[^\w\s]", ""),
                128),
            1024);


    for (int i = 0; i < tokenList.Count; i++)
        await memory.SaveInformationAsync(collectionName, tokenList[i], $"paragraph{i}");
}

// Create a new chat
IChatCompletionService ai = kernel.GetRequiredService<IChatCompletionService>();

ChatHistory chat = new("You are an AI assistant that helps people find information about a book.");
StringBuilder builder = new();

//var arguments = new KernelArguments();

// Q&A loop
while (true)
{
    Console.Write("Question: ");
    string question = Console.ReadLine()!;

    builder.Clear();
    await foreach (var result in memory.SearchAsync(collectionName, question, limit: 50))
        builder.AppendLine(result.Metadata.Text);

    int contextToRemove = -1;
    if (builder.Length != 0)
    {
        builder.Insert(0, "Here's some additional information: ");
        contextToRemove = chat.Count;
        chat.AddUserMessage(builder.ToString());
    }

    chat.AddUserMessage(question);

    builder.Clear();
    await foreach (var message in ai.GetStreamingChatMessageContentsAsync(chat))
    {
        Console.Write(message);
        builder.Append(message.Content);
    }
    Console.WriteLine();
    chat.AddAssistantMessage(builder.ToString());

    if (contextToRemove >= 0) chat.RemoveAt(contextToRemove);
    Console.WriteLine();
}