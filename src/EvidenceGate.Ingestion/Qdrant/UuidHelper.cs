using System.Security.Cryptography;

namespace EvidenceGate.Ingestion.Qdrant;

public static class UuidHelper
{
    public static Guid GenerarUuidDeterministico(string texto)
    {
        byte[] hash = MD5.HashData(System.Text.Encoding.UTF8.GetBytes(texto));
        return new Guid(hash);
    }
}