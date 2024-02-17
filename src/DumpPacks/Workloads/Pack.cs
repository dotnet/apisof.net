using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

public sealed class Pack
{
    public Pack(PackKind kind,
                string version,
                IReadOnlyDictionary<string, string>? aliasTo)
    {
        aliasTo ??= ReadOnlyDictionary<string, string>.Empty;
        
        Kind = kind;
        Version = version;
        AliasTo = aliasTo.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public PackKind Kind { get; }
    public string Version { get; }
    
    [JsonPropertyName("alias-to")]
    public IReadOnlyDictionary<string, string> AliasTo { get; }
}