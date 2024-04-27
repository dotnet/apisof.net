using System.ComponentModel.DataAnnotations;

namespace Terrajobst.ApiCatalog.ActionsRunner;

public sealed class ApisOfDotNetWebHookOptions
{
    [Required]
    public required string GenCatalogWebHookUrl { get; init; }

    [Required]
    public required string GenCatalogWebHookSecret { get; init; }
}