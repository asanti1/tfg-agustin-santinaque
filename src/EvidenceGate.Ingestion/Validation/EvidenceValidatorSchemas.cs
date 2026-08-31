using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.Models.Messages;

namespace EvidenceGate.Ingestion.Validation;

public static class EvidenceValidatorSchemas
{
    private const string NombreLlamado1 = "reportar_evaluacion_cobertura";
    private const string NombreLlamado2 = "reportar_evaluacion_contradiccion";

    public static Tool CrearLlamado1Tool()
    {
        string jsonSchema = """
        {
          "type": "object",
          "properties": {
            "explicacion_preliminar": {
              "type": "string",
              "description": "OBLIGATORIO, nunca vacío: 1-2 oraciones explicando tu veredicto de cobertura, incluso si la cobertura es completa."
            },
            "partes_no_cubiertas_json": {
              "type": "string",
              "description": "OBLIGATORIO: Array JSON de strings con lo que falta cubrir. Si cobertura_global es 'completa', usar un array vacío '[]' explícito."
            },
            "cobertura_global": {
              "type": "string",
              "enum": ["completa", "parcial", "sin_cobertura"]
            },
            "vigencia_ok": { "type": "boolean" },
            "requiere_verificacion_contradiccion": { "type": "boolean" },
            "fragmentos_json": {
              "type": "string",
              "description": "Array JSON con la evaluación de cada fragmento. Cada elemento: {\"id\": string, \"cobertura\": \"completa\"|\"parcial\"|\"sin_cobertura\", \"partes_de_la_pregunta_cubiertas\": [string], \"grupo_supuesto_hecho\": string}"
            }
          },
          "required": ["explicacion_preliminar", "partes_no_cubiertas_json", "cobertura_global", "vigencia_ok", "requiere_verificacion_contradiccion", "fragmentos_json"],
          "additionalProperties": false
        }
        """;


        var schemaDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonSchema)!;

        return new Tool
        {
            Name = NombreLlamado1,
            Description = "Reporta el resultado de evaluar cobertura y mismo supuesto de hecho de los fragmentos.",
            InputSchema = InputSchema.FromRawUnchecked(schemaDict),
            Strict = true
        };
    }

    public static Tool CrearLlamado2Tool()
    {
        string jsonSchema = """
        {
          "type": "object",
          "properties": {
            "resultado": { "type": "string", "enum": ["D1", "D2", "sin_contradiccion"] },
            "hecho_central": { "type": "string", "description": "Resumen breve del hecho central compartido." },
            "atributo_en_disputa": { "type": "string", "description": "Solo si D2: qué atributo difiere entre las fuentes." },
            "fuentes_en_conflicto_json": { "type": "string", "description": "Array JSON de strings con los Ids de los fragmentos en conflicto." },
            "explicacion": { "type": "string", "description": "OBLIGATORIO, nunca vacío." }
          },
          "required": ["resultado", "hecho_central", "atributo_en_disputa", "fuentes_en_conflicto_json", "explicacion"],
          "additionalProperties": false
        }
        """;

        var schemaDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonSchema)!;

        return new Tool
        {
            Name = NombreLlamado2,
            Description = "Reporta si existe contradicción total, parcial, o ninguna entre fragmentos del mismo supuesto de hecho.",
            InputSchema = InputSchema.FromRawUnchecked(schemaDict),
            Strict = true
        };
    }

    public static ToolChoice Llamado1ToolChoice => new(new ToolChoiceTool { Name = NombreLlamado1 });
    public static ToolChoice Llamado2ToolChoice => new(new ToolChoiceTool { Name = NombreLlamado2 });
}