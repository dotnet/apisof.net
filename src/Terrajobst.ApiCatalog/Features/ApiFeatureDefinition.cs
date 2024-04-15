using System.Security.Cryptography;

namespace Terrajobst.ApiCatalog.Features;

public abstract class ApiFeatureDefinition : FeatureDefinition
{
    protected static Guid CombineGuids(Guid g1, Guid g2)
    {
        var bytes = (Span<byte>) stackalloc byte[32];
        g1.TryWriteBytes(bytes);
        g2.TryWriteBytes(bytes[16..]);

        var hashBytes = (Span<byte>)stackalloc byte[16];
        MD5.HashData(bytes, hashBytes);
        return new Guid(hashBytes);
    }

    public abstract Guid GetFeatureId(Guid api);
}