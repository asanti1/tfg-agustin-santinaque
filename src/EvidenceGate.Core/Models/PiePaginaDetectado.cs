using System.Diagnostics.CodeAnalysis;

namespace EvidenceGate.Core.Models;

public class PiePaginaDetectado
{
    public required string Version { get; set; }
    public required string Comunicacion { get; set; }
    public required string Vigencia { get; set; }
    public required string Pagina { get; set; }
    public int PosicionEnDocumento { get; set; }

    [SetsRequiredMembers]
    public PiePaginaDetectado(string version, string comunicacion, string vigencia, string pagina, int posicionEnDocumento)
    {
        Version = version;
        Comunicacion = comunicacion;
        Vigencia = vigencia;
        Pagina = pagina;
        PosicionEnDocumento = posicionEnDocumento;
    }
}