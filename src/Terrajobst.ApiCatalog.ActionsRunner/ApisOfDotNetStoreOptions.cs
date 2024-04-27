using System.ComponentModel.DataAnnotations;

namespace Terrajobst.ApiCatalog.ActionsRunner;

public sealed class ApisOfDotNetStoreOptions
{
    [Required]
    public required string AzureStorageConnectionString { get; init; }
}