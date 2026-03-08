// Azure Open AI Chat Client (Using Semantic Kernel)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Identity;
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

        const int MaxHistoryChars = 8000;
        readonly Queue<string> _historyEntries = new();
        int _historyChars;

        // Constructor
        public SKHelper(string model, string azureEndpoint, string tenantId, string clientId, string clientSecret, int maxTokens, double temperature, double topP = 0.5)
        {
            // In the constructor, wrap credential creation in try-catch for diagnostics
            try
            {
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                
                // Test the credential explicitly before using it
                var tokenRequestContext = new Azure.Core.TokenRequestContext(
                    new[] { "https://cognitiveservices.azure.com/.default" });
                var token = credential.GetToken(tokenRequestContext);

                _kernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(model, azureEndpoint, credential)
                    .Build();
            }
            catch (AuthenticationFailedException ex)
            {
                // This will give you the detailed error message from Azure AD
                throw new Exception($"Authentication failed: {ex.Message}", ex);
            }

            OpenAIPromptExecutionSettings requestSettings = new()
            {
                //MaxTokens = maxTokens,
                //Temperature = temperature,
                //TopP = topP,
            };

            _chatFunction = _kernel.CreateFunctionFromPrompt(_skPrompt, requestSettings);
            _arguments = new();

            InitContext();
        }

        // Init Context
        public void InitContext()
        {
            _historyEntries.Clear();
            _historyChars = 0;
            _arguments["history"] = string.Empty;
        }

        // Get Full Context
        public string? GetFullContext()
        {
            return _arguments["history"]?.ToString();
        }

        // Chat
        public async Task<string> Chat(string input)
        {
            _arguments["userInput"] = input;

            var answer = await _chatFunction.InvokeAsync(_kernel, _arguments);
            string answerText = answer?.ToString() ?? string.Empty;

            AppendHistory($"User: {input}\nChatBot: {answerText}\n");

            return answerText;
        }

        void AppendHistory(string entry)
        {
            _historyEntries.Enqueue(entry);
            _historyChars += entry.Length;

            while (_historyChars > MaxHistoryChars && _historyEntries.Count > 0)
            {
                var removed = _historyEntries.Dequeue();
                _historyChars -= removed.Length;
            }

            _arguments["history"] = string.Concat(_historyEntries);
        }
    }
}
