using EvidenceGate.Ingestion.Embeddings;
using global::Qdrant.Client;
using EvidenceGate.Ingestion.Qdrant;
using EvidenceGate.Ingestion.Validation;
using Microsoft.Extensions.Logging;
using EvidenceGate.Core.Models;
using EvidenceGate.Ingestion;

DotNetEnv.Env.Load();
string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Falta OPENAI_API_KEY en .env");
string anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
    ?? throw new InvalidOperationException("Falta ANTHROPIC_API_KEY en .env");

var qdrantClient = new QdrantClient("localhost", 6334);


using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information); // subí a Debug si querés ver el JSON crudo
});

var embeddingClientLogger = loggerFactory.CreateLogger<EmbeddingClient>();
var embeddingClientParaIndexer = new EmbeddingClient(apiKey, embeddingClientLogger);

var retriever = new QdrantRetriever(qdrantClient, embeddingClientParaIndexer);
var validatorLogger = loggerFactory.CreateLogger<EvidenceValidatorClient>();
var validatorClient = new EvidenceValidatorClient(anthropicKey, validatorLogger);
var indexerLogger = loggerFactory.CreateLogger<QdrantIndexer>();
var indexer = new QdrantIndexer(qdrantClient, embeddingClientParaIndexer, indexerLogger);

string pregunta = "¿Cuál es el plazo máximo para acreditar transferencias inmediatas?";
List<Chunk> chunksRecuperados = await retriever.BuscarAsync(pregunta, "bcra_pagos", topK: 5);

Console.WriteLine($"\n--- Evidence Gate completo para: \"{pregunta}\" ---\n");

EvidenceGateResult resultado = await validatorClient.EvaluarAsync(pregunta, chunksRecuperados);

Console.WriteLine($"Tipo de evidencia: {resultado.Tipo}");
Console.WriteLine($"¿Puede generar respuesta?: {resultado.PuedeGenerar}");
Console.WriteLine($"Explicación para el usuario: {resultado.ExplicacionParaUsuario}");
Console.WriteLine($"Fuentes utilizadas: {string.Join(", ", resultado.FuentesUtilizadas)}");