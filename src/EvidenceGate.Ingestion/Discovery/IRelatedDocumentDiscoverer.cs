namespace EvidenceGate.Ingestion.Discovery;

public interface IRelatedDocumentDiscoverer
{
    Task<List<SeedEntry>> DescubrirRelacionadosAsync(string rutaPdfBase, string tema);
}