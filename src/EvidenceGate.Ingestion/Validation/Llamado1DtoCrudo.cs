namespace EvidenceGate.Ingestion.Validation;

public class Llamado1DtoCrudo
{
    public string? FragmentosJson { get; set; }
    public string CoberturaGlobal { get; set; } = "";
    public string? PartesNoCubiertasJson { get; set; }
    public bool VigenciaOk { get; set; }
    public bool RequiereVerificacionContradiccion { get; set; }
    public string? ExplicacionPreliminar { get; set; }
}