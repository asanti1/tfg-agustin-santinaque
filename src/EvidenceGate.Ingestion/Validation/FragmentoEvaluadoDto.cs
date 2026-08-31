namespace EvidenceGate.Ingestion.Validation;
public class FragmentoEvaluadoDto
{
    public string Id { get; set; } = "";
    public string Cobertura { get; set; } = "";
    public List<string> PartesDeLaPreguntaCubiertas { get; set; } = new();
    public string? GrupoSupuestoHecho { get; set; }
}