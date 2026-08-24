using System.Text.Json;
using System.Text.Json.Serialization;
using EvidenceGate.Core.Models;

namespace EvidenceGate.Ingestion;

public class BcraScraper
{
    private readonly HttpClient _http;
    private readonly string _corpusDir;
    private readonly BcraComunicacionesDiscoverer _discoverer;

    private JsonSerializerOptions _opciones;

    public BcraScraper(HttpClient http, string corpusDir)
    {
        _http = http;
        _corpusDir = corpusDir;
        _opciones = new JsonSerializerOptions { WriteIndented = true };
        _opciones.Converters.Add(new JsonStringEnumConverter());
        _discoverer = new BcraComunicacionesDiscoverer();
    }

    public async Task DescargarCorpusAsync(string tema)
    {
        List<SeedEntry> entradas = BcraDocumentSeed.Documentos.Where(d => d.Tema == tema).ToList();
        if (entradas.Count == 0) return;

        string dirCorpus = Path.Combine(_corpusDir, tema);
        List<DocumentoMetadata> metadataExistente = CargarMetadataExistente(dirCorpus);
        List<DocumentoMetadata> metadataActualizada = [.. metadataExistente];
        List<SeedEntry> documentosADescubrir = new List<SeedEntry>();

        string dirDocs = Path.Combine(dirCorpus, "documentos");
        Directory.CreateDirectory(dirDocs);

        foreach (SeedEntry entrada in entradas)
        {
            var pathFull = Path.Combine(dirDocs, $"{entrada.Id}.pdf");
            if (await DescargarUnaEntradaAsync(entrada, pathFull, metadataActualizada) && entrada.Tipo == TipoDocumento.TextoOrdenado)
                documentosADescubrir.AddRange(await _discoverer.DescubrirRelacionadosAsync(pathFull, tema));
        }

        foreach (SeedEntry entrada in documentosADescubrir)
        {
            var pathFull = Path.Combine(dirDocs, $"{entrada.Id}.pdf");
            await DescargarUnaEntradaAsync(entrada, pathFull, metadataActualizada);
        }

        GuardarMetadata(dirCorpus, metadataActualizada);
        Console.WriteLine($"Procesadas {entradas.Count} entradas del seed + {documentosADescubrir.Count} descubiertas para el tema '{tema}'");
    }

    private async Task<bool> DescargarUnaEntradaAsync(SeedEntry entrada, string rutaDestino, List<DocumentoMetadata> metadataActualizada)
    {
        try
        {
            var bytes = await _http.GetByteArrayAsync(entrada.UrlOrigen);
            await File.WriteAllBytesAsync(rutaDestino, bytes);

            var metadata = new DocumentoMetadata
            {
                Id = entrada.Id,
                NombreArchivo = $"{entrada.Id}.pdf",
                NombreDescriptivo = entrada.NombreDescriptivo,
                Tipo = entrada.Tipo,
                UrlOrigen = entrada.UrlOrigen,
                Version = entrada.Version,
                Vigente = entrada.Vigente,
                FechaVigenciaDesde = entrada.FechaVigenciaDesde,
                ReemplazaA = entrada.ReemplazaA,
                Tema = entrada.Tema
            };
            metadataActualizada.RemoveAll(m => m.Id == entrada.Id);
            metadataActualizada.Add(metadata);
            return true;
        }
        catch (HttpRequestException err)
        {
            Console.WriteLine($"Hubo un error = {err.Message}");
            return false;
        }

    }

    private List<DocumentoMetadata> CargarMetadataExistente(string carpetaCorpus)
    {
        var rutaMetadata = Path.Combine(carpetaCorpus, "metadata.json");
        if (!File.Exists(rutaMetadata))
            return new List<DocumentoMetadata>();

        var json = File.ReadAllText(rutaMetadata);
        return JsonSerializer.Deserialize<List<DocumentoMetadata>>(json, _opciones) ?? new List<DocumentoMetadata>();
    }

    private void GuardarMetadata(string carpetaCorpus, List<DocumentoMetadata> metadata)
    {
        var rutaMetadata = Path.Combine(carpetaCorpus, "metadata.json");

        var json = JsonSerializer.Serialize(metadata, _opciones);
        File.WriteAllText(rutaMetadata, json);
    }
}