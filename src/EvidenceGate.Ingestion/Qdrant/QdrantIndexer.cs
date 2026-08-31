using Qdrant.Client;
using Qdrant.Client.Grpc;
using EvidenceGate.Core.Models;
using EvidenceGate.Ingestion.Embeddings;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using EvidenceGate.Ingestion.Chunking;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using EvidenceGate.Core.Exceptions;

namespace EvidenceGate.Ingestion.Qdrant;

public class QdrantIndexer
{
    private readonly QdrantClient _qdrant;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<QdrantIndexer> _logger;
    private const string NombreColeccion = "evidence_gate_chunks";

    public QdrantIndexer(QdrantClient qdrant, EmbeddingClient embeddingClient, ILogger<QdrantIndexer> logger)
    {
        _qdrant = qdrant;
        _embeddingClient = embeddingClient;
        _logger = logger;
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

        _logger.LogInformation("Colección {Coleccion} creada", NombreColeccion);
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
        _logger.LogInformation("Indexados {Cantidad} chunks en Qdrant", puntos.Count);
    }

    public async Task<int> IndexarCorpusCompletoAsync(string tema, string corpusDir)
    {
        string dirCorpus = Path.Combine(corpusDir, tema);
        string rutaMetadata = Path.Combine(dirCorpus, "metadata.json");

        if (!File.Exists(rutaMetadata))
        {
            _logger.LogWarning("No se encontró metadata.json para el tema {Tema}", tema);
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
                _logger.LogWarning("Documento {Id} omitido: PDF no encontrado en {Ruta}", metadata.Id, rutaPdf);
                documentosConError++;
                continue;
            }

            try
            {
                using PdfDocument documento = PdfDocument.Open(rutaPdf);
                string textoCompleto = string.Join("\n", documento.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));

                if (string.IsNullOrWhiteSpace(textoCompleto))
                {
                    _logger.LogWarning("Documento {Id} omitido: texto extraído vacío", metadata.Id);
                    documentosConError++;
                    continue;
                }

                var chunks = chunker.Chunkear(textoCompleto, metadata);
                todosLosChunks.AddRange(chunks);
                documentosProcesados++;
                _logger.LogInformation("Documento {Id} procesado: {Cantidad} chunks generados", metadata.Id, chunks.Count);
            }
            catch (Exception ex)
            {
                var extraccionError = new ExtraccionException($"Error extrayendo/chunkeando {metadata.Id}", ex);
                _logger.LogError(extraccionError, "Error procesando documento {Id}", metadata.Id);
                documentosConError++;
            }
        }

        _logger.LogInformation("Resumen de indexado: {Procesados} procesados, {ConError} con error, {TotalChunks} chunks totales", documentosProcesados, documentosConError, todosLosChunks.Count);
        await CrearColeccionSiNoExisteAsync();
        await IndexarChunksAsync(todosLosChunks);

        return todosLosChunks.Count;
    }
}