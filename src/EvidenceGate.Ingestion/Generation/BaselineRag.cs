using EvidenceGate.Ingestion.Qdrant;

namespace EvidenceGate.Ingestion.Generation;

/// <summary>
/// RAG baseline: retrieval + generación, sin Evidence Gate. Punto de comparación
/// contra el sistema propuesto (ver sección 6 del documento principal).
/// </summary>
public class BaselineRag
{
    private readonly QdrantRetriever _retriever;
    private readonly ClaudeClient _claudeClient;

    public BaselineRag(QdrantRetriever retriever, ClaudeClient claudeClient)
    {
        _retriever = retriever;
        _claudeClient = claudeClient;
    }

    public async Task<string> ResponderAsync(string pregunta, string tema)
    {
        var chunks = await _retriever.BuscarAsync(pregunta, tema);

        string fragmentos = string.Join("\n\n", chunks.Select(c =>
            $"[{c.DocumentoId} - {c.ReferenciaEstructural}]: {c.Texto}"));

        string prompt = $@"Respondé la siguiente pregunta basándote ÚNICAMENTE en los fragmentos de normativa que se incluyen a continuación. Citá la fuente (documento y sección) de cada afirmación que hagas.
        PREGUNTA: {pregunta} 
        FRAGMENTOS: {fragmentos}
        RESPUESTA:";

        return await _claudeClient.GenerarAsync(prompt);
    }
}