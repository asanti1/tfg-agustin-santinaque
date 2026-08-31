using System.Diagnostics.CodeAnalysis;

namespace EvidenceGate.Core.Models;

public class SegmentoNumerado
{
    public required string Numeracion { get; set; }   // "3.7.5"
    public required int Nivel { get; set; }            // 3 (cantidad de partes separadas por punto)
    public required string Titulo { get; set; }         // "Requisitos de seguridad"
    public required string Texto { get; set; }           // el contenido completo de ese punto
    public int PosicionEnDocumento { get; set; }         // offset en el texto unificado, útil para ordenar
    
    [SetsRequiredMembers]
    public SegmentoNumerado(string numeracion, int nivel, string titulo, string texto, int posicionEnDocumento)
    {
        Numeracion = numeracion;
        Nivel = nivel;
        Titulo = titulo;
        Texto = texto;
        PosicionEnDocumento = posicionEnDocumento;
    }
}