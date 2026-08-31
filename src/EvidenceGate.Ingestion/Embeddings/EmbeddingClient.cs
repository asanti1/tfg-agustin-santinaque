using OpenAI.Embeddings;

namespace EvidenceGate.Ingestion.Embeddings;

/// <summary>
/// Cliente para generar embeddings vía la API de OpenAI, con batching para
/// no exceder el rate limit y minimizar la cantidad de requests.
/// </summary>
public class EmbeddingClient
{
    private readonly OpenAI.Embeddings.EmbeddingClient _client;

    public EmbeddingClient(string apiKey, string modelo = "text-embedding-3-small")
    {
        _client = new OpenAI.Embeddings.EmbeddingClient(modelo, apiKey);
    }

    public async Task<List<float[]>> GenerarEmbeddingsAsync(List<string> textos, int tamañoLote = 50)
    {
        List<float[]> resultado = new();

        for (int i = 0; i < textos.Count; i += tamañoLote)
        {
            var lote = textos.Skip(i).Take(tamañoLote).ToList();
            var respuesta = await _client.GenerateEmbeddingsAsync(lote);

            foreach (var embedding in respuesta.Value)
            {
                resultado.Add(embedding.ToFloats().ToArray());
            }

            Console.WriteLine($"Lote {i / tamañoLote + 1}: {lote.Count} textos embebidos");
        }

        return resultado;
    }
}