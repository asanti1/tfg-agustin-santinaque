namespace EvidenceGate.Core.Models;

public enum TipoEvidencia
{
    /// <summary>Cobertura directa, sin conflicto, versión vigente.</summary>
    A,

    /// <summary>Evidencia parcial — cubre una parte de la pregunta, no toda.</summary>
    B,

    /// <summary>Sin evidencia relevante.</summary>
    C,

    /// <summary>Contradicción total entre fuentes sobre el hecho central.</summary>
    D1,

    /// <summary>Contradicción parcial — el hecho central coincide, difiere un atributo.</summary>
    D2,

    /// <summary>Evidencia con cobertura directa pero de versión derogada.</summary>
    E,
}