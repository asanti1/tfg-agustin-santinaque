namespace EvidenceGate.Core.Models;

/// <summary>Nivel de cobertura de un fragmento o de la evidencia global frente a una pregunta.</summary>
public enum NivelCobertura
{
    Completa,
    Parcial,
    SinCobertura
}

/// <summary>Resultado de la evaluación de un fragmento individual en el Llamado 1.</summary>
public class FragmentoEvaluado
{
    public required string Id { get; set; }
    public NivelCobertura Cobertura { get; set; }
    public List<string> PartesDeLaPreguntaCubiertas { get; set; } = new();

    /// <summary>Letra de grupo (A, B, ...) para fragmentos que aplican al mismo supuesto de hecho.
    /// Null si el fragmento no tiene cobertura.</summary>
    public string? GrupoSupuestoHecho { get; set; }
}

/// <summary>
/// Resultado del Llamado 1 (cobertura + mismo supuesto de hecho).
/// Ver sección 9.5 del documento principal, prompt del Llamado 1.
/// </summary>
public class Validator1Result
{
    public List<FragmentoEvaluado> Fragmentos { get; set; } = new();
    public NivelCobertura CoberturaGlobal { get; set; }
    public List<string> PartesNoCubiertas { get; set; } = new();
    public bool VigenciaOk { get; set; }
    public bool RequiereVerificacionContradiccion { get; set; }
    public required string ExplicacionPreliminar { get; set; }
}

/// <summary>Resultado posible del Llamado 2 (comparación de contradicción).</summary>
public enum ResultadoContradiccion
{
    D1,
    D2,
    SinContradiccion
}

/// <summary>
/// Resultado del Llamado 2 (contradicción total vs. parcial).
/// Ver sección 9.5 del documento principal, prompt del Llamado 2.
/// Solo se ejecuta si Validator1Result.RequiereVerificacionContradiccion == true.
/// </summary>
public class Validator2Result
{
    public ResultadoContradiccion Resultado { get; set; }
    public string? HechoCentral { get; set; }
    public string? AtributoEnDisputa { get; set; }
    public List<string> FuentesEnConflicto { get; set; } = new();
    public required string Explicacion { get; set; }
}

/// <summary>
/// Resultado final combinado del Evidence Gate: el tipo de evidencia clasificado
/// y la explicación que se le muestra al usuario. Es el output del flujo de decisión
/// descripto en la sección 9.5 (pseudocódigo) del documento principal.
/// </summary>
public class EvidenceGateResult
{
    public required string Pregunta { get; set; }
    public TipoEvidencia Tipo { get; set; }
    public required string ExplicacionParaUsuario { get; set; }
    public List<string> FuentesUtilizadas { get; set; } = new();

    /// <summary>True si el sistema debe generar una respuesta (Tipo A o D2). False si debe abstenerse
    /// o mostrar evidencia con salvedades sin generar libremente (B, C, D1, E).</summary>
    public bool PuedeGenerar => Tipo == TipoEvidencia.A || Tipo == TipoEvidencia.D2;
}
