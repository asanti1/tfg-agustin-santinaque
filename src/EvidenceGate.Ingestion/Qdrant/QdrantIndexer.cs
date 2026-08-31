using Qdrant.Client;
using Qdrant.Client.Grpc;
using EvidenceGate.Core.Models;
using EvidenceGate.Ingestion.Embeddings;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using EvidenceGate.Ingestion.Chunking;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvidenceGate.Ingestion.Qdrant;

public class QdrantIndexer
{
    private readonly QdrantClient _qdrant;
    private readonly EmbeddingClient _embeddingClient;
    private const string NombreColeccion = "evidence_gate_chunks";

    public QdrantIndexer(QdrantClient qdrant, EmbeddingClient embeddingClient)
    {
        _qdrant = qdrant;
        _embeddingClient = embeddingClient;
    }

    public async Task CrearColeccionSiNoExisteAsync()
    {
        var colecciones = await _qdrant.ListCollectionsAsync();
        if (colecciones.Contains(NombreColeccion)) return;

        await _qdrant.CreateCollectionAsync(NombreColeccion, new VectorParams
        {
            Size = 1536,
            Distance = Distance.Cosine
        });

        Console.WriteLine($"Colección '{NombreColeccion}' creada.");
    }

    public async Task IndexarChunksAsync(List<Chunk> chunks)
    {
        var textos = chunks.Select(c => c.Texto).ToList();
        var embeddings = await _embeddingClient.GenerarEmbeddingsAsync(textos);

        var puntos = new List<PointStruct>();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var payload = new Dictionary<string, Value>
            {
                ["chunk_id"] = chunk.Id,
                ["documento_id"] = chunk.DocumentoId,
                ["tema"] = chunk.Tema ?? "",
                ["vigente"] = chunk.Vigente ?? true,
                ["texto"] = chunk.Texto,
                ["referencia_estructural"] = chunk.ReferenciaEstructural ?? "",
                ["version_norma"] = chunk.VersionNorma ?? "",
                ["comunicacion_origen"] = chunk.ComunicacionOrigen ?? "",
                ["fecha_vigencia_desde"] = chunk.FechaVigenciaDesde ?? "",
                ["contexto_anterior"] = chunk.ContextoAnterior ?? "",
                ["contexto_siguiente"] = chunk.ContextoSiguiente ?? ""
            };

            puntos.Add(new PointStruct
            {
                Id = new PointId { Uuid = UuidHelper.GenerarUuidDeterministico(chunk.Id).ToString() },
                Vectors = embeddings[i],
                Payload = { payload }
            });
        }

        await _qdrant.UpsertAsync(NombreColeccion, puntos);
        Console.WriteLine($"Indexados {puntos.Count} chunks en Qdrant.");
    }

    public async Task<int> IndexarCorpusCompletoAsync(string tema, string corpusDir)
    {
        string dirCorpus = Path.Combine(corpusDir, tema);
        string rutaMetadata = Path.Combine(dirCorpus, "metadata.json");

        if (!File.Exists(rutaMetadata))
        {
            Console.WriteLine($"No se encontró metadata.json para el tema '{tema}'.");
            return 0;
        }

        var opciones = new JsonSerializerOptions();
        opciones.Converters.Add(new JsonStringEnumConverter());
        var json = File.ReadAllText(rutaMetadata);
        var documentos = JsonSerializer.Deserialize<List<DocumentoMetadata>>(json, opciones) ?? new List<DocumentoMetadata>();

        var chunker = new BcraChunker();
        var todosLosChunks = new List<Chunk>();
        int documentosProcesados = 0;
        int documentosConError = 0;

        foreach (var metadata in documentos)
        {
            string rutaPdf = Path.Combine(dirCorpus, "documentos", metadata.NombreArchivo);

            if (!File.Exists(rutaPdf))
            {
                Console.WriteLine($"  [omitido] {metadata.Id}: PDF no encontrado en {rutaPdf}");
                documentosConError++;
                continue;
            }

            try
            {
                using PdfDocument documento = PdfDocument.Open(rutaPdf);
                string textoCompleto = string.Join("\n", documento.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));

                if (string.IsNullOrWhiteSpace(textoCompleto))
                {
                    Console.WriteLine($"  [omitido] {metadata.Id}: texto extraído vacío");
                    documentosConError++;
                    continue;
                }

                var chunks = chunker.Chunkear(textoCompleto, metadata);
                todosLosChunks.AddRange(chunks);
                documentosProcesados++;
                Console.WriteLine($"  [ok] {metadata.Id}: {chunks.Count} chunks generados");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [error] {metadata.Id}: {ex.Message}");
                documentosConError++;
            }
        }

        Console.WriteLine($"\nDocumentos procesados: {documentosProcesados} | Con error: {documentosConError} | Total chunks a indexar: {todosLosChunks.Count}");

        await CrearColeccionSiNoExisteAsync();
        await IndexarChunksAsync(todosLosChunks);

        return todosLosChunks.Count;
    }
}