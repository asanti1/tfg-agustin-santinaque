namespace EvidenceGate.Core.Models;

/// <summary>
/// Metadata de un documento del corpus. Se persiste en metadata.json dentro de
/// cada carpeta de corpus (ver sección "corpus intercambiable" del documento principal).
/// Esta metadata es la que permite el pre-filtrado por vigencia/versión en Qdrant
/// (ver sección 10, nota de diseño sobre Qdrant vs Chroma).
/// </summary>
public class DocumentoMetadata
{
    /// <summary>Identificador único del documento (ej: "t-snp-spd", "A8303").</summary>
    public required string Id { get; set; }

    /// <summary>Nombre del archivo PDF en la carpeta documentos/.</summary>
    public required string NombreArchivo { get; set; }

    /// <summary>Nombre descriptivo (ej: "Sistema Nacional de Pagos - Servicios de Pago").</summary>
    public required string NombreDescriptivo { get; set; }

    public required TipoDocumento Tipo { get; set; }

    /// <summary>URL de origen en bcra.gob.ar, para trazabilidad y para poder re-descargar.</summary>
    public required string UrlOrigen { get; set; }

    /// <summary>Número de versión del documento (para Textos Ordenados con múltiples snapshots históricos).</summary>
    public int Version { get; set; } = 1;

    /// <summary>Fecha de vigencia del documento (formato ISO 8601: "2025-08-18").</summary>
    public string? FechaVigenciaDesde { get; set; }

    /// <summary>Fecha en que dejó de estar vigente, si corresponde. Null si sigue vigente.</summary>
    public string? FechaVigenciaHasta { get; set; }

    /// <summary>True si es la versión vigente actual. False si es un snapshot histórico o fue derogado.</summary>
    public bool Vigente { get; set; } = true;

    /// <summary>Id del documento que este reemplaza, si aplica (para trazar el historial de versiones).</summary>
    public string? ReemplazaA { get; set; }

    /// <summary>Tema/corpus al que pertenece (ej: "pagos", "seguridad") — para cohesión temática y filtrado.</summary>
    public required string Tema { get; set; }

    /// <summary>Fecha en que se descargó/scrapeó el documento.</summary>
    public DateTime FechaDescarga { get; set; } = DateTime.UtcNow;
}
