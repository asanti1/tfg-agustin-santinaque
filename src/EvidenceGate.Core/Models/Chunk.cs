namespace EvidenceGate.Core.Models;

/// <summary>
/// Fragmento de texto indexado. Incluye contexto adyacente para reducir el riesgo
/// de "chunks huérfanos" (ver sección 9.4 del documento principal).
/// </summary>
public class Chunk
{
    /// <summary>Identificador único del chunk (ej: "t-snp-spd_sec1_pt2").</summary>
    public required string Id { get; set; }

    /// <summary>Id del documento (DocumentoMetadata.Id) al que pertenece.</summary>
    public required string DocumentoId { get; set; }

    /// <summary>Texto del fragmento.</summary>
    public required string Texto { get; set; }

    /// <summary>Texto del chunk anterior en el documento, si existe. Se pasa al validator como contexto.</summary>
    public string? ContextoAnterior { get; set; }

    /// <summary>Texto del chunk siguiente en el documento, si existe.</summary>
    public string? ContextoSiguiente { get; set; }

    /// <summary>Referencia estructural dentro del documento (ej: "Sección 5, Punto 5.2.1").</summary>
    public string? ReferenciaEstructural { get; set; }

    /// <summary>Embedding del chunk (se completa en la etapa de indexado, no en la de chunking).</summary>
    public float[]? Embedding { get; set; }
}
