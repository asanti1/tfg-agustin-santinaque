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

    /// <summary>Versión de la norma según el pie de página más cercano al inicio del chunk (ej: "5").</summary>
    public string? VersionNorma { get; set; }

    /// <summary>Número de la Comunicación "A" que originó esta versión (ej: "8303").</summary>
    public string? ComunicacionOrigen { get; set; }

    /// <summary>Fecha de vigencia según el pie de página (formato DD/MM/AAAA).</summary>
    public string? FechaVigenciaDesde { get; set; }

    /// <summary>Número de página del documento original donde se ubica este chunk.</summary>
    public string? Pagina { get; set; }

    /// <summary>Embedding del chunk (se completa en la etapa de indexado, no en la de chunking).</summary>
    public float[]? Embedding { get; set; }
    /// <summary>Tema/corpus al que pertenece el documento (ej: "bcra_pagos"). Viene de DocumentoMetadata.</summary>
    public string? Tema { get; set; }

    /// <summary>Si la versión del documento de origen está vigente. Viene de DocumentoMetadata.</summary>
    public bool? Vigente { get; set; }
}
