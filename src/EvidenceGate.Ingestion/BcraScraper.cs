namespace EvidenceGate.Ingestion;
public class BcraScraper
{
    private readonly HttpClient _http;
    private readonly string _corpusDir;

    public BcraScraper(HttpClient http, string corpusDir)
    {
        _http = http;
        _corpusDir = corpusDir;
    }

    public async Task DescargarCorpusAsync(string tema)
    {
        List<SeedEntry> entradas = BcraDocumentSeed.Documentos.Where(d => d.Tema == tema).ToList();

        if (entradas.Count == 0) return;

        string dirCorpus = Path.Combine(_corpusDir, tema);
        string dirDocs = Path.Combine(dirCorpus, "documentos");
        Directory.CreateDirectory(dirDocs);

        foreach (SeedEntry entrada in entradas)
        {
            string nombreArchivo = $"{entrada.Id}.pdf";
            string rutaDestino = Path.Combine(dirDocs, nombreArchivo);

            try
            {
                var bytes = await _http.GetByteArrayAsync(entrada.UrlOrigen);
                await File.WriteAllBytesAsync(rutaDestino, bytes);
            }
            catch (HttpRequestException err)
            {
                Console.WriteLine($"Hubo un error = {err.Message}");
            }
        }
        Console.WriteLine($"Encontradas {entradas.Count} entradas para el tema '{tema}'");
    }
}