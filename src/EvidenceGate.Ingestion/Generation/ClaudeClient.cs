using Anthropic;
using Anthropic.Models.Messages;

namespace EvidenceGate.Ingestion.Generation;

public class ClaudeClient
{
    private readonly AnthropicClient _client;
    private const string Modelo = "claude-sonnet-5";

    public ClaudeClient(string apiKey)
    {
        _client = new AnthropicClient { ApiKey = apiKey };
    }

    public async Task<string> GenerarAsync(string prompt)
    {
        MessageCreateParams parametros = new()
        {
            Model = Modelo,
            MaxTokens = 1024,
            Messages = [ new() { Role = Role.User, Content = prompt } ]
        };

        var respuesta = await _client.Messages.Create(parametros);
        return respuesta.ToString() ?? "";
    }
}