using System.Text.RegularExpressions;
using EvidenceGate.Core.Models;
namespace EvidenceGate.Ingestion.Chunking;

public class BcraChunker
{
    private const string MarcadorInicioContenido = "Índice";
    private const string MarcadorFinIndice = "Tabla de correlaciones";
    private const string MarcadorFinContenido = "ORIGEN DE LAS DISPOSICIONES CONTENIDAS EN EL TEXTO ORDENADO";
    private static readonly Regex PatronNumeracion = new(@"(?:^|\n)\s*(\d+(?:\.\d+)*)\.\s+([^\n]+)", RegexOptions.Compiled);
    private static readonly Regex PatronPiePagina = new(
        @"Versión:\s*(\d+)a\.\s*COMUNICACIÓN\s*[\u201C""]A[\u201D""]\s*(\d+)\s*Vigencia:\s*(\d{2}/\d{2}/\d{4})\s*Página\s*(\d+)",
        RegexOptions.Compiled);

    public List<SegmentoNumerado> DetectarSegmentos(string textoCompleto)
    {
        string contenidoNormativo = ExtraerContenidoNormativo(textoCompleto);
        var matches = PatronNumeracion.Matches(contenidoNormativo).Cast<Match>().ToList();

        List<SegmentoNumerado> segmentos = new();
        for (int i = 0; i < matches.Count; i++)
        {
            var actual = matches[i];
            int inicioTexto = actual.Index;
            int finTexto = (i + 1 < matches.Count) ? matches[i + 1].Index : contenidoNormativo.Length;

            string textoDelSegmento = contenidoNormativo.Substring(inicioTexto, finTexto - inicioTexto);
            segmentos.Add(new SegmentoNumerado(matches[i].Groups[1].Value,
                matches[i].Groups[1].Value.Split('.').Length,
                matches[i].Groups[2].Value,
                textoDelSegmento,
                inicioTexto));
        }

        return segmentos;
    }


    private string ExtraerContenidoNormativo(string textoCompleto)
    {
        int inicioIndice = textoCompleto.IndexOf(MarcadorInicioContenido);
        if (inicioIndice == -1) return textoCompleto;

        int finIndice = textoCompleto.IndexOf(MarcadorFinIndice, inicioIndice);
        if (finIndice == -1) return textoCompleto;

        int inicioContenidoReal = finIndice + MarcadorFinIndice.Length;

        int finContenido = textoCompleto.IndexOf(MarcadorFinContenido, inicioContenidoReal);
        if (finContenido == -1) finContenido = textoCompleto.Length;

        return textoCompleto.Substring(inicioContenidoReal, finContenido - inicioContenidoReal);
    }


    public Tuple<int, int> EncontrarMejorRacha(List<SegmentoNumerado> listaDeNiveles)
    {
        int mejorInicio = 0;
        int mejorLargo = 0;
        int inicioActual = 0;
        int largoActual = 1;

        for (int i = 1; i < listaDeNiveles.Count; i++)
        {
            int numeroActual = int.Parse(listaDeNiveles[i].Numeracion);
            int numeroAnterior = int.Parse(listaDeNiveles[i - 1].Numeracion);

            if (numeroActual >= numeroAnterior)
            {
                largoActual++;
            }
            else
            {
                if (largoActual > mejorLargo)
                {
                    mejorLargo = largoActual;
                    mejorInicio = inicioActual;
                }
                inicioActual = i;
                largoActual = 1;
            }
        }

        if (largoActual > mejorLargo)
        {
            mejorLargo = largoActual;
            mejorInicio = inicioActual;
        }

        int mejorFin = mejorInicio + mejorLargo - 1;
        return new Tuple<int, int>(mejorInicio, mejorFin);
    }

    public List<SegmentoNumerado> AgruparChunks(List<SegmentoNumerado> segmentos, int umbralCaracteres = 1800)
    {
        int i = 0;
        List<SegmentoNumerado> resultado = new List<SegmentoNumerado>();

        while (i < segmentos.Count)
        {
            SegmentoNumerado x = segmentos[i];
            string textoAcumulado = x.Texto;
            int j = i + 1;

            while (j < segmentos.Count && segmentos[j].Nivel > x.Nivel)
            {
                textoAcumulado += segmentos[j].Texto;
                j++;
            }

            if (textoAcumulado.Length > umbralCaracteres)
            {
                int k = i + 1;
                while (k < j)
                {
                    SegmentoNumerado y = segmentos[k];
                    string textoSubgrupo = y.Texto;
                    int m = k + 1;

                    while (m < j && segmentos[m].Nivel > y.Nivel)
                    {
                        textoSubgrupo += segmentos[m].Texto;
                        m++;
                    }

                    resultado.Add(new SegmentoNumerado(y.Numeracion, y.Nivel, y.Titulo, textoSubgrupo, y.PosicionEnDocumento));
                    k = m;
                }
            }
            else
            {
                resultado.Add(new SegmentoNumerado(x.Numeracion, x.Nivel, x.Titulo, textoAcumulado, x.PosicionEnDocumento));
            }

            i = j;
        }

        return resultado;
    }

    public List<Chunk> ConvertirAChunks(List<SegmentoNumerado> segmentosAgrupados, List<PiePaginaDetectado> piesDePagina, DocumentoMetadata metadata)
    {
        List<Chunk> chunks = new List<Chunk>();
        Dictionary<string, int> contadorPorNumeracion = new Dictionary<string, int>();

        for (int i = 0; i < segmentosAgrupados.Count; i++)
        {
            SegmentoNumerado segmento = segmentosAgrupados[i];

            string idBase = $"{metadata.Id}_{segmento.Numeracion}";
            string idFinal;

            if (contadorPorNumeracion.ContainsKey(segmento.Numeracion))
            {
                contadorPorNumeracion[segmento.Numeracion]++;
                idFinal = $"{idBase}-dup{contadorPorNumeracion[segmento.Numeracion]}";
            }
            else
            {
                contadorPorNumeracion[segmento.Numeracion] = 1;
                idFinal = idBase;
            }

            string? contextoAnterior = (i > 0) ? segmentosAgrupados[i - 1].Titulo : null;
            string? contextoSiguiente = (i < segmentosAgrupados.Count - 1) ? segmentosAgrupados[i + 1].Titulo : null;
            string referenciaEstructural = string.Join(".", segmento.Numeracion.Split('.').Take(2));
            PiePaginaDetectado? piePagina = piesDePagina
                .Where(p => p.PosicionEnDocumento >= segmento.PosicionEnDocumento)
                .OrderBy(p => p.PosicionEnDocumento)
                .FirstOrDefault();
            chunks.Add(new Chunk
            {
                Id = idFinal,
                DocumentoId = metadata.Id,
                Texto = segmento.Texto,
                ContextoAnterior = contextoAnterior,
                ContextoSiguiente = contextoSiguiente,
                ReferenciaEstructural = referenciaEstructural,
                VersionNorma = piePagina?.Version,
                ComunicacionOrigen = piePagina?.Comunicacion,
                FechaVigenciaDesde = piePagina?.Vigencia,
                Pagina = piePagina?.Pagina,
                Tema = metadata.Tema,
                Vigente = metadata.Vigente
            });
        }

        return chunks;
    }

    public List<PiePaginaDetectado> DetectarPiesDePagina(string textoCompleto)
    {
        List<PiePaginaDetectado> resultado = new();

        foreach (Match m in PatronPiePagina.Matches(textoCompleto))
        {
            resultado.Add(new PiePaginaDetectado(
                m.Groups[1].Value,
                m.Groups[2].Value,
                m.Groups[3].Value,
                m.Groups[4].Value,
                m.Index
            ));
        }

        return resultado;
    }

    /// <summary>
    /// Chunkea cualquier documento normativo del BCRA: intenta detectar estructura
    /// numerada (Textos Ordenados, o Comunicaciones que reproducen estructura, como A8303),
    /// y si no encuentra ninguna, cae a chunking por tamaño fijo como red de seguridad.
    /// </summary>
    public List<Chunk> Chunkear(string textoCompleto, DocumentoMetadata metadata, int tamañoFijoCaracteres = 1800)
    {
        var segmentos = DetectarSegmentos(textoCompleto);

        if (segmentos.Count > 0)
        {
            // Tiene estructura numerada reconocible - mismo pipeline que un Texto Ordenado
            var segmentosAgrupados = AgruparChunks(segmentos);
            var piesDePagina = DetectarPiesDePagina(textoCompleto);
            return ConvertirAChunks(segmentosAgrupados, piesDePagina, metadata);
        }

        // Sin estructura numerada - red de seguridad: chunking por tamaño fijo
        return ChunkearPorTamañoFijo(textoCompleto, metadata, tamañoFijoCaracteres);
    }

    private List<Chunk> ChunkearPorTamañoFijo(string textoCompleto, DocumentoMetadata metadata, int tamañoFijoCaracteres)
    {
        List<Chunk> chunks = new List<Chunk>();
        int cantidadPartes = (int)Math.Ceiling((double)textoCompleto.Length / tamañoFijoCaracteres);

        for (int i = 0; i < cantidadPartes; i++)
        {
            int inicio = i * tamañoFijoCaracteres;
            int largo = Math.Min(tamañoFijoCaracteres, textoCompleto.Length - inicio);

            chunks.Add(new Chunk
            {
                Id = $"{metadata.Id}_parte{i + 1}",
                DocumentoId = metadata.Id,
                Texto = textoCompleto.Substring(inicio, largo),
                ContextoAnterior = (i > 0) ? $"Parte {i} de {cantidadPartes}" : null,
                ContextoSiguiente = (i < cantidadPartes - 1) ? $"Parte {i + 2} de {cantidadPartes}" : null,
                ReferenciaEstructural = metadata.Id,
                Tema = metadata.Tema,
                Vigente = metadata.Vigente
            });
        }

        return chunks;
    }
}