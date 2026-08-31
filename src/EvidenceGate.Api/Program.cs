using EvidenceGate.Ingestion.Embeddings;
using global::Qdrant.Client;
using EvidenceGate.Ingestion.Qdrant;
using EvidenceGate.Ingestion.Validation;
using EvidenceGate.Core.Models;

DotNetEnv.Env.Load();
string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("Falta OPENAI_API_KEY en .env");
string anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
    ?? throw new InvalidOperationException("Falta ANTHROPIC_API_KEY en .env");

var qdrantClient = new QdrantClient("localhost", 6334);
var embeddingClientParaIndexer = new EmbeddingClient(apiKey);
var retriever = new QdrantRetriever(qdrantClient, embeddingClientParaIndexer);
var validatorClient = new EvidenceValidatorClient(anthropicKey);

string pregunta = "¿Cuál es el plazo máximo para acreditar transferencias inmediatas?";
var chunksRecuperados = await retriever.BuscarAsync(pregunta, "bcra_pagos", topK: 5);

Console.WriteLine($"\n--- Evidence Gate completo para: \"{pregunta}\" ---\n");

var resultado = await validatorClient.EvaluarAsync(pregunta, chunksRecuperados);

Console.WriteLine($"Tipo de evidencia: {resultado.Tipo}");
Console.WriteLine($"¿Puede generar respuesta?: {resultado.PuedeGenerar}");
Console.WriteLine($"Explicación para el usuario: {resultado.ExplicacionParaUsuario}");
Console.WriteLine($"Fuentes utilizadas: {string.Join(", ", resultado.FuentesUtilizadas)}");



// ---------- Tipo C: sin cobertura ----------
var chunksC = new List<Chunk>
{
    new Chunk { Id = "test_C1", DocumentoId = "docTest", Texto = "Las entidades deben informar el tipo de cambio aplicado en operaciones de comercio exterior.", Tema = "bcra_pagos", Vigente = true }
};
var resultadoC = await validatorClient.EvaluarAsync("¿Cuál es el capital mínimo requerido para una entidad financiera?", chunksC);
Console.WriteLine($"\n=== TIPO C esperado ===\nTipo: {resultadoC.Tipo} | PuedeGenerar: {resultadoC.PuedeGenerar}\nExplicación: {resultadoC.ExplicacionParaUsuario}");

// ---------- Tipo E: versión derogada ----------
var chunksE = new List<Chunk>
{
    new Chunk { Id = "test_E1", DocumentoId = "docTest", Texto = "El plazo máximo para acreditar transferencias inmediatas es de 30 segundos.", Tema = "bcra_pagos", Vigente = false }
};
var resultadoE = await validatorClient.EvaluarAsync("¿Cuál es el plazo máximo para acreditar transferencias inmediatas?", chunksE);
Console.WriteLine($"\n=== TIPO E esperado ===\nTipo: {resultadoE.Tipo} | PuedeGenerar: {resultadoE.PuedeGenerar}\nExplicación: {resultadoE.ExplicacionParaUsuario}");

// ---------- Tipo D1: contradicción total ----------
var chunksD1 = new List<Chunk>
{
    new Chunk { Id = "FUENTE_A", DocumentoId = "docTest", Texto = "Las entidades financieras tienen prohibido cobrar comisión por el mantenimiento de cuentas sueldo.", Tema = "bcra_pagos", Vigente = true },
    new Chunk { Id = "FUENTE_B", DocumentoId = "docTest", Texto = "Las entidades financieras están autorizadas a cobrar comisión por el mantenimiento de cuentas sueldo, con un tope máximo mensual.", Tema = "bcra_pagos", Vigente = true }
};
var resultadoD1 = await validatorClient.EvaluarAsync("¿Se puede cobrar comisión por mantenimiento de cuenta sueldo?", chunksD1);
Console.WriteLine($"\n=== TIPO D1 esperado ===\nTipo: {resultadoD1.Tipo} | PuedeGenerar: {resultadoD1.PuedeGenerar}\nExplicación: {resultadoD1.ExplicacionParaUsuario}");

// ---------- Tipo B: cobertura parcial ----------
var chunksB = new List<Chunk>
{
    new Chunk { Id = "test_B1", DocumentoId = "docTest", Texto = "Las entidades financieras deben notificar al cliente en caso de fraude detectado en su cuenta.", Tema = "bcra_pagos", Vigente = true }
};
var resultadoB = await validatorClient.EvaluarAsync("¿Cuál es el plazo y el monto máximo de reintegro en casos de fraude?", chunksB);
Console.WriteLine($"\n=== TIPO B esperado ===\nTipo: {resultadoB.Tipo} | PuedeGenerar: {resultadoB.PuedeGenerar}\nExplicación: {resultadoB.ExplicacionParaUsuario}");