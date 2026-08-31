using EvidenceGate.Core.Models;
using EvidenceGate.Ingestion.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace EvidenceGate.Ingestion.Qdrant;

public class QdrantRetriever
{
    private readonly QdrantClient _qdrant;
    private readonly EmbeddingClient _eClient;
    private const string NombreColeccion = "evidence_gate_chunks";

    public QdrantRetriever(QdrantClient qdrant, EmbeddingClient eClient)
    {
        _qdrant = qdrant;
        _eClient = eClient;
    }

    public async Task<List<Chunk>> BuscarAsync(string pregunta, string tema, int topK = 5)
    {
        var embeddings = await _eClient.GenerarEmbeddingsAsync(new List<string> { pregunta });
        float[] vectorPregunta = embeddings[0];

        var filtro = new Filter
        {
            Must = {
                new Condition { Field = new FieldCondition { Key = "tema", Match = new Match { Keyword = tema } } },
                new Condition { Field = new FieldCondition { Key = "vigente", Match = new Match { Boolean = true } } }
            }
        };

        var puntos = await _qdrant.QueryAsync(
            collectionName: NombreColeccion,
            query: vectorPregunta,
            filter: filtro,
            limit: (ulong)topK
        );

        return puntos.Select(ConvertirPuntoAChunk).ToList();
    }

    private Chunk ConvertirPuntoAChunk(ScoredPoint punto)
    {
        var payload = punto.Payload;
        return new Chunk
        {
            Id = payload["chunk_id"].StringValue,
            DocumentoId = payload["documento_id"].StringValue,
            Texto = payload["texto"].StringValue,
            Tema = payload["tema"].StringValue,
            Vigente = payload["vigente"].BoolValue,
            ReferenciaEstructural = payload["referencia_estructural"].StringValue,
            VersionNorma = payload["version_norma"].StringValue,
            ComunicacionOrigen = payload["comunicacion_origen"].StringValue,
            FechaVigenciaDesde = payload["fecha_vigencia_desde"].StringValue,
            ContextoAnterior = payload["contexto_anterior"].StringValue,
            ContextoSiguiente = payload["contexto_siguiente"].StringValue
        };
    }
}