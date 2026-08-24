using EvidenceGate.Core.Models;

namespace EvidenceGate.Ingestion;

public record SeedEntry(
    string Id,
    string NombreDescriptivo,
    string UrlOrigen,
    TipoDocumento Tipo,
    string Tema,
    int Version = 1,
    bool Vigente = true,
    string? FechaVigenciaDesde = null,
    string? ReemplazaA = null
);

public static class BcraDocumentSeed
{
    public static readonly List<SeedEntry> Documentos = new()
    {
        new SeedEntry("t-snp-spd", "Sistema Nacional de Pagos - Servicios de Pago", "https://www.bcra.gob.ar/archivos/Pdfs/Texord/t-snp-spd.pdf", TipoDocumento.TextoOrdenado, "bcra_pagos" ),
        new SeedEntry("t-seguef", "Medidas Mínimas de Seguridad en Entidades Financieras", "https://www.bcra.gob.ar/archivos/Pdfs/texord/t-seguef.pdf", TipoDocumento.TextoOrdenado, "bcra_seguridad" )
    };
}