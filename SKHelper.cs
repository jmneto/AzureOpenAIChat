// Azure Open AI Chat Client (Using Semantic Kernel)

using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AzureOpenAIChat
{
    internal class SKHelper
    {
        // Semantic Kernel Objects
        Kernel _kernel;
        KernelArguments _arguments;
        KernelFunction _chatFunction;

        // ChatBot Prompt
        const string _skPrompt = @"
        ChatBot can have a conversation with you about any topic.
        It can give explicit instructions or say 'I don't know' if it does not have an answer.

        {{$history}}
        User: {{$userInput}}
        ChatBot:";

        // Constructor
        public SKHelper(string model, string azureEndpoint, string apiKey, int maxTokens, double temperature, double topP = 0.5)
        {
            // Semantic Kernel
            _kernel = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(model, azureEndpoint, apiKey).Build();

            // Request Setting
            OpenAIPromptExecutionSettings requestSettings = new()
            {
                MaxTokens = maxTokens,
                Temperature = temperature,
                TopP = topP,
            };

            // Chat Function
            _chatFunction = _kernel.CreateFunctionFromPrompt(_skPrompt, requestSettings);
            _arguments = new();

            // Init Context
            InitContext();
        }

        // Init Context
        public void InitContext()
        {
            _arguments["history"] = "";
        }

        // Chat
        public async Task<string> Chat(string input)
        {
            // Update Context
            _arguments["userInput"] = input;

            // Invoke Chat Function
            var answer = await _chatFunction.InvokeAsync(_kernel, _arguments);

            // Update Context
            _arguments["history"] += $"\nUser: {input}\nChatBot: {answer}\n";

            // Return Answer
            return answer.ToString();
        }
    }
}
