using EvidenceGate.Ingestion;

IEnumerable<string> temas = args.Length == 0 ? ["all"] : args;

HttpClient http = new HttpClient();

string dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "corpus");
dir = Path.GetFullPath(dir);
Console.WriteLine($"Carpeta de corpus resuelta: {dir}");

var scraper = new BcraScraper(http, dir);

temas = temas.First() == "all" ? BcraDocumentSeed.Documentos.Select(d => d.Tema).Distinct() : temas;
{
    foreach (string tema in temas)
    {
        await scraper.DescargarCorpusAsync(tema);
    }
}
