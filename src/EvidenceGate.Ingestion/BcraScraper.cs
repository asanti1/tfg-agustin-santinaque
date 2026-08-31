using System.Text.Json;
using System.Text.Json.Serialization;
using EvidenceGate.Core.Models;
using Microsoft.Extensions.Logging;
using EvidenceGate.Core.Exceptions;

namespace EvidenceGate.Ingestion;

public class BcraScraper
{
    private readonly HttpClient _http;
    private readonly string _corpusDir;
    private readonly BcraComunicacionesDiscoverer _discoverer;
    private readonly ILogger<BcraScraper> _logger;

    private JsonSerializerOptions _opciones;

    public BcraScraper(HttpClient http, string corpusDir, ILogger<BcraScraper> logger)
    {
        _http = http;
        _corpusDir = corpusDir;
        _logger = logger;
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
        _logger.LogInformation("Procesadas {EntradasSeed} entradas del seed + {Descubiertas} descubiertas para el tema {Tema}", entradas.Count, documentosADescubrir.Count, tema);
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
            var descargaError = new DescargaException($"Error descargando {entrada.Id} desde {entrada.UrlOrigen}", err);
            _logger.LogError(descargaError, "Error descargando {Id} desde {Url}", entrada.Id, entrada.UrlOrigen);
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