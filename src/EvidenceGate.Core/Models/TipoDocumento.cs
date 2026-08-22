namespace EvidenceGate.Core.Models;

/// <summary>
/// Tipo de documento normativo. Determina la estrategia de chunking a aplicar
/// (ver sección 9.4 del documento principal: chunking adaptativo).
/// </summary>
public enum TipoDocumento
{
    /// <summary>
    /// Texto Ordenado: documento consolidado, largo (~30 páginas).
    /// Requiere chunking estructural por artículo/inciso.
    /// </summary>
    TextoOrdenado,

    /// <summary>
    /// Comunicación "A": documento corto (~media página a pocas páginas).
    /// Se indexa como documento completo o por punto/artículo, sin chunking agresivo.
    /// </summary>
    ComunicacionA
}
