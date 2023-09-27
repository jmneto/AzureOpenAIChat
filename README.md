# Azure Open AI Chat Client

This is a C# Azure Open AI Chat Client that uses the Semantic Kernel.

It can be used to test diverse LLM models like GPT-3, GPT-3.5 Turbo or GPT4 models.

## Usage

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Build the project.
4. Run the project.

## Dependencies

- Microsoft.SemanticKernel

## Configuration

The following parameters must be set in the constructor of the `SKHelper` class:

- `model`: The model to use for chat completion.
- `azureEndpoint`: The Azure endpoint URL.
- `apiKey`: The API key for the Azure endpoint.
- `maxTokens`: The maximum number of tokens in the chat response.
- `temperature`: The temperature for the chat response generation.

## Credits

This project was developed by jmneto in September 2024.