namespace EvidenceGate.Core.Exceptions;

public abstract class EvidenceGateException : Exception
{
    protected EvidenceGateException(string message) : base(message) { }
    protected EvidenceGateException(string message, Exception innerException) : base(message, innerException) { }
}

public class DescargaException : EvidenceGateException
{
    public DescargaException(string message, Exception innerException) : base(message, innerException) { }
}

public class ExtraccionException : EvidenceGateException
{
    public ExtraccionException(string message, Exception innerException) : base(message, innerException) { }
}

public class ValidatorException : EvidenceGateException
{
    public ValidatorException(string message) : base(message) { }
    public ValidatorException(string message, Exception innerException) : base(message, innerException) { }
}