// jmneto Azure Open AI Chat Client (Using Semantic Kernel)
// Sept 2024 - Version 1.0

using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace AzureOpenAIChat
{
    internal class SKHelper
    {
        // Semantic Kernel Objects
        IKernel _kernel;
        SKContext _context;
        ISKFunction _chatFunction;

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
            _kernel = (new KernelBuilder()).WithAzureChatCompletionService(model, azureEndpoint, apiKey).Build();

            // Prompt Template
            PromptTemplateConfig promptConfig = new PromptTemplateConfig
            {
                Completion =
                    {
                        MaxTokens = maxTokens,
                        Temperature = temperature,
                        TopP = topP
                    }
            };

            // Chat Function
            var promptTemplate = new PromptTemplate(_skPrompt, promptConfig, _kernel);
            var functionConfig = new SemanticFunctionConfig(promptConfig, promptTemplate);
            _chatFunction = _kernel.RegisterSemanticFunction("ChatBot", "Chat", functionConfig);
            _context = _kernel.CreateNewContext();

            // Init Context
            InitContext();
        }

        // Init Context
        public void InitContext()
        {
            _context.Variables["history"] = "";
        }

        // Chat
        public async Task<string> Chat(string input)
        {
            // Update Context
            _context.Variables["userInput"] = input;

            // Invoke Chat Function
            var answer = await _chatFunction.InvokeAsync(_context);

            // Update Context
            _context.Variables["history"] += $"\nUser: {input}\nMelody: {answer}\n";

            // Return Answer
            return _context.ToString();
        }
    }
}
