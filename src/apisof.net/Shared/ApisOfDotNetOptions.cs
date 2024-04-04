using System.ComponentModel.DataAnnotations;

namespace ApisOfDotNet.Shared;

public sealed class ApisOfDotNetOptions
{
    [Required]
    public required string AzureStorageConnectionString { get; init; }

    [Required]
    public required string GenCatalogWebHookSecret { get; init; }
}