﻿using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

public sealed class Workload
{
    public Workload(bool @abstract,
                    string description,
                    IReadOnlyList<string>? packs,
                    IReadOnlyList<string>? platforms,
                    IReadOnlyList<string>? extends)
    {
        Abstract = @abstract;
        Description = description;
        Packs = packs ?? ReadOnlyCollection<string>.Empty;
        Platforms = platforms ?? ReadOnlyCollection<string>.Empty;
        Extends = extends ?? ReadOnlyCollection<string>.Empty;
    }

    [JsonIgnore]
    public string Name { get; internal set; } = string.Empty;
    public bool Abstract { get; }
    public string Description { get; }
    public IReadOnlyList<string> Packs { get; }
    public IReadOnlyList<string> Platforms { get; }
    public IReadOnlyList<string> Extends { get; }
}