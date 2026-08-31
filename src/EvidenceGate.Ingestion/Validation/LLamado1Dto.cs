namespace EvidenceGate.Ingestion.Validation;
public class Llamado1Dto
{
    public List<FragmentoEvaluadoDto> Fragmentos { get; set; } = new();
    public string CoberturaGlobal { get; set; } = "";
    public List<string> PartesNoCubiertas { get; set; } = new();
    public bool VigenciaOk { get; set; }
    public bool RequiereVerificacionContradiccion { get; set; }
    public string ExplicacionPreliminar { get; set; } = "";
}