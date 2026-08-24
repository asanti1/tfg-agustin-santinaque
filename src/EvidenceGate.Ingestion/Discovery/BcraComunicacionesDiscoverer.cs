using System.Text.RegularExpressions;
using EvidenceGate.Ingestion;
using EvidenceGate.Ingestion.Discovery;
using EvidenceGate.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

public class BcraComunicacionesDiscoverer : IRelatedDocumentDiscoverer
{
    // El patrón solo captura Comunicaciones tipo "A" (asignación de la letra fija en el regex,
    // no capturada como grupo). Las Comunicaciones "B" (circulares informativas) y "C" (fe de
    // erratas) que aparecen en el mismo bloque del BCRA se descartan a propósito: no son la
    // categoría normativa que define TipoDocumento.ComunicacionA en este proyecto (ver sección
    // 8/9 del documento principal). Si en algún momento se necesitara contemplar B/C, el cambio
    // es puntual: capturar la letra como grupo -> [\u201C""]([ABC])[\u201D""]... -- y decidir
    // qué hacer con cada caso (nuevo TipoDocumento, o filtro explícito post-captura).
    private static readonly Regex PatronComunicacion = new(
    @"[\u201C""]A[\u201D""]\s+(\d+):\s*([^\n]+?)\.",
    RegexOptions.Compiled);
    private const string MarcadorOrigen = "Comunicaciones que dieron origen";
    private const string MarcadorVinculadas = "Comunicaciones vinculadas a esta norma";
    private const string MarcadorNormativaRelacionada = "Normativa relacionada";
    public async Task<List<SeedEntry>> DescubrirRelacionadosAsync(string rutaPdfBase, string tema)
    {
        var resultado = new List<SeedEntry>();

        using PdfDocument documento = PdfDocument.Open(rutaPdfBase);
        string textoCompleto = string.Join("\n", documento.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));

        ExtraerComunicacionesDeBloque(textoCompleto, MarcadorOrigen, MarcadorVinculadas, resultado, tema);

        ExtraerComunicacionesDeBloque(textoCompleto, MarcadorVinculadas, MarcadorNormativaRelacionada, resultado, tema);

        return resultado;
    }

    private static void ExtraerComunicacionesDeBloque(string textoCompleto, string marcadorInicio, string marcadorFin, List<SeedEntry> resultado, string tema)
    {
        int indexInicio = textoCompleto.IndexOf(marcadorInicio);
        if (indexInicio == -1) return;

        int indexFin = textoCompleto.IndexOf(marcadorFin, indexInicio + marcadorInicio.Length);
        if (indexFin == -1) indexFin = textoCompleto.Length;

        string bloque = textoCompleto.Substring(indexInicio, indexFin - indexInicio);
        string bloqueNormalizado = Regex.Replace(bloque, @"\s+", " ");
        foreach (Match m in PatronComunicacion.Matches(bloqueNormalizado))
        {
            string numero = m.Groups[1].Value;
            string descripcion = m.Groups[2].Value;
            resultado.Add(new SeedEntry($"A{numero}",
            descripcion, $"https://www.bcra.gob.ar/archivos/Pdfs/comytexord/A{numero}.pdf",
            TipoDocumento.ComunicacionA,
            tema));
        }
    }

}