using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;
using EvidenceGate.Core.Models;
using Microsoft.Extensions.Logging;
using EvidenceGate.Core.Exceptions;

namespace EvidenceGate.Ingestion.Validation;

public class EvidenceValidatorClient
{
    private readonly AnthropicClient _client;
    private const string Modelo = "claude-sonnet-5";
    private readonly ILogger<EvidenceValidatorClient> _logger;

    private static readonly JsonSerializerOptions OpcionesDeserializacion = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };


    public EvidenceValidatorClient(string apiKey, ILogger<EvidenceValidatorClient> logger)
    {
        _client = new AnthropicClient { ApiKey = apiKey };
        _logger = logger;
    }

    public async Task<Validator1Result> EjecutarLlamado1Async(string prompt)
    {
        MessageCreateParams parametros = new()
        {
            Model = Modelo,
            MaxTokens = 1024,
            Messages = [new() { Role = Role.User, Content = prompt }],
            Tools = [EvidenceValidatorSchemas.CrearLlamado1Tool()],
            ToolChoice = EvidenceValidatorSchemas.Llamado1ToolChoice
        };

        var respuesta = await _client.Messages.Create(parametros);


        var toolUseBlock = respuesta.Content.FirstOrDefault(c => c.Type.GetString() == "tool_use");

        if (toolUseBlock == null || !toolUseBlock.TryPickToolUse(out var toolUse))
            throw new ValidatorException("Claude no devolvió un tool_use en el Llamado 1.");

        string json = JsonSerializer.Serialize(toolUse.Input);

        _logger.LogDebug("JSON crudo del Llamado 1: {Json}", json);

        var crudo = JsonSerializer.Deserialize<Llamado1DtoCrudo>(json, OpcionesDeserializacion)
            ?? throw new ValidatorException("No se pudo deserializar la respuesta del Llamado 1.");

        var fragmentos = string.IsNullOrWhiteSpace(crudo.FragmentosJson) ? new List<FragmentoEvaluado>()
            : JsonSerializer.Deserialize<List<FragmentoEvaluado>>(crudo.FragmentosJson, OpcionesDeserializacion) ?? new();

        var partesNoCubiertas = string.IsNullOrWhiteSpace(crudo.PartesNoCubiertasJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(crudo.PartesNoCubiertasJson, OpcionesDeserializacion) ?? new();

        return new Validator1Result
        {
            Fragmentos = fragmentos,
            CoberturaGlobal = MapearCobertura(crudo.CoberturaGlobal),
            PartesNoCubiertas = partesNoCubiertas,
            VigenciaOk = crudo.VigenciaOk,
            RequiereVerificacionContradiccion = crudo.RequiereVerificacionContradiccion,
            ExplicacionPreliminar = crudo.ExplicacionPreliminar ?? "(el validator no proporcionó una explicación)"
        };
    }

    private static NivelCobertura MapearCobertura(string valor) => valor switch
    {
        "completa" => NivelCobertura.Completa,
        "parcial" => NivelCobertura.Parcial,
        "sin_cobertura" => NivelCobertura.SinCobertura,
        _ => throw new ValidatorException($"Valor de cobertura desconocido: {valor}")
    };


    public async Task<Validator2Result> EjecutarLlamado2Async(string prompt)
    {
        MessageCreateParams parametros = new()
        {
            Model = Modelo,
            MaxTokens = 1024,
            Messages = [new() { Role = Role.User, Content = prompt }],
            Tools = [EvidenceValidatorSchemas.CrearLlamado2Tool()],
            ToolChoice = EvidenceValidatorSchemas.Llamado2ToolChoice
        };

        var respuesta = await _client.Messages.Create(parametros);

        var toolUseBlock = respuesta.Content.FirstOrDefault(c => c.Type.GetString() == "tool_use");

        if (toolUseBlock == null || !toolUseBlock.TryPickToolUse(out var toolUse))
            throw new ValidatorException("Claude no devolvió un tool_use en el Llamado 2.");

        string json = JsonSerializer.Serialize(toolUse.Input);

        var crudo = JsonSerializer.Deserialize<Llamado2DtoCrudo>(json, OpcionesDeserializacion)
            ?? throw new ValidatorException("No se pudo deserializar la respuesta del Llamado 2.");

        var fuentesEnConflicto = string.IsNullOrWhiteSpace(crudo.FuentesEnConflictoJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(crudo.FuentesEnConflictoJson, OpcionesDeserializacion) ?? new();

        return new Validator2Result
        {
            Resultado = MapearResultadoContradiccion(crudo.Resultado),
            HechoCentral = crudo.HechoCentral,
            AtributoEnDisputa = crudo.AtributoEnDisputa,
            FuentesEnConflicto = fuentesEnConflicto,
            Explicacion = crudo.Explicacion
        };
    }

    private static ResultadoContradiccion MapearResultadoContradiccion(string valor) => valor switch
    {
        "D1" => ResultadoContradiccion.D1,
        "D2" => ResultadoContradiccion.D2,
        "sin_contradiccion" => ResultadoContradiccion.SinContradiccion,
        _ => throw new ValidatorException($"Valor de resultado de contradicción desconocido: {valor}")
    };


    public async Task<EvidenceGateResult> EvaluarAsync(string pregunta, List<Chunk> chunksRecuperados)
    {
        string promptL1 = ArmarPromptLlamado1(pregunta, chunksRecuperados);
        var r1 = await EjecutarLlamado1Async(promptL1);

        if (r1.CoberturaGlobal == NivelCobertura.SinCobertura)
        {
            return ArmarResultado(pregunta, TipoEvidencia.C,
                $"No existe evidencia suficiente en la documentación disponible para responder esta consulta. {r1.ExplicacionPreliminar}",
                new List<string>());
        }

        if (r1.CoberturaGlobal == NivelCobertura.Parcial)
        {
            return ArmarResultado(pregunta, TipoEvidencia.B,
                $"La evidencia disponible cubre parcialmente la pregunta. Falta: {string.Join(", ", r1.PartesNoCubiertas)}. {r1.ExplicacionPreliminar}",
                r1.Fragmentos.Select(f => f.Id).ToList());
        }

        if (!r1.VigenciaOk)
        {
            return ArmarResultado(pregunta, TipoEvidencia.E,
                $"La evidencia encontrada corresponde a una versión que ya no está vigente. {r1.ExplicacionPreliminar}",
                r1.Fragmentos.Select(f => f.Id).ToList());
        }

        if (r1.RequiereVerificacionContradiccion)
        {
            var idsDelGrupoConflictivo = r1.Fragmentos
                .Where(f => f.GrupoSupuestoHecho != null)
                .GroupBy(f => f.GrupoSupuestoHecho)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Select(f => f.Id))
                .ToList();

            if (idsDelGrupoConflictivo.Count == 0)
            {
                return ArmarResultado(pregunta, TipoEvidencia.A, r1.ExplicacionPreliminar,
                    r1.Fragmentos.Select(f => f.Id).ToList());
            }

            string promptL2 = ArmarPromptLlamado2(pregunta, chunksRecuperados, idsDelGrupoConflictivo);
            var r2 = await EjecutarLlamado2Async(promptL2);

            return r2.Resultado switch
            {
                ResultadoContradiccion.D1 => ArmarResultado(pregunta, TipoEvidencia.D1,
                    $"Existe contradicción entre las fuentes sobre el hecho central: {r2.Explicacion}",
                    r2.FuentesEnConflicto),

                ResultadoContradiccion.D2 => ArmarResultado(pregunta, TipoEvidencia.D2,
                    $"Las fuentes coinciden en lo esencial, pero difieren en: {r2.AtributoEnDisputa}",
                    r2.FuentesEnConflicto),

                _ => ArmarResultado(pregunta, TipoEvidencia.A,
                    $"{r1.ExplicacionPreliminar} (verificado: sin contradicción entre fuentes — {r2.Explicacion})",
                    r1.Fragmentos.Select(f => f.Id).ToList())
            };
        }

        return ArmarResultado(pregunta, TipoEvidencia.A, r1.ExplicacionPreliminar,
            r1.Fragmentos.Select(f => f.Id).ToList());
    }

    private static EvidenceGateResult ArmarResultado(string pregunta, TipoEvidencia tipo, string explicacion, List<string> fuentes)
    {
        return new EvidenceGateResult
        {
            Pregunta = pregunta,
            Tipo = tipo,
            ExplicacionParaUsuario = explicacion,
            FuentesUtilizadas = fuentes
        };
    }

    private static string ArmarPromptLlamado1(string pregunta, List<Chunk> chunks)
    {
        string fragmentos = FormatearFragmentos(chunks, incluirMetadataCompleta: true);

        return $@"
PREGUNTA: {pregunta}

FRAGMENTOS RECUPERADOS:
{fragmentos}

Evaluá la cobertura de estos fragmentos respecto a la pregunta, agrupando por 
mismo supuesto de hecho, siguiendo las instrucciones del tool proporcionado.";
    }

    private static string ArmarPromptLlamado2(string pregunta, List<Chunk> chunksOriginales, List<string> idsDelGrupoConflictivo)
    {
        var chunksDelGrupo = chunksOriginales.Where(c => idsDelGrupoConflictivo.Contains(c.Id)).ToList();
        string fragmentos = FormatearFragmentos(chunksDelGrupo, incluirMetadataCompleta: false);

        return $@"
PREGUNTA ORIGINAL: {pregunta}

FRAGMENTOS A COMPARAR (mismo supuesto de hecho confirmado):
{fragmentos}

Determiná si existe contradicción total, parcial, o ninguna entre estos 
fragmentos, siguiendo las instrucciones del tool proporcionado. Completá 
siempre el campo explicacion, nunca lo dejes vacío.";
    }

    private static string FormatearFragmentos(List<Chunk> chunks, bool incluirMetadataCompleta)
    {
        return string.Join("\n\n", chunks.Select(c => incluirMetadataCompleta
            ? $"ID: {c.Id}\nDocumento: {c.DocumentoId}\nVigente: {c.Vigente}\nSección: {c.ReferenciaEstructural}\nTexto: {c.Texto}"
            : $"ID: {c.Id}\nDocumento: {c.DocumentoId}\nTexto: {c.Texto}"));
    }
}