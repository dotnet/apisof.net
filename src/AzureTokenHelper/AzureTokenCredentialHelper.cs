using Azure.Core;
using Azure.Identity;

namespace BlobClientHelper;

public class AzureTokenCredentialHelper : TokenCredential
{
    private static readonly string[] StorageAccountScope = new[] { "https://storage.azure.com/.default" };

    private readonly TokenCredential _accessTokenProvider;
    public AzureTokenCredentialHelper(TokenCredential tokenCredential)
    {
        ThrowIfNull(tokenCredential);
        _accessTokenProvider = tokenCredential;
    }
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return _accessTokenProvider.GetToken(new TokenRequestContext(StorageAccountScope), cancellationToken);
    }
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return _accessTokenProvider.GetTokenAsync(new TokenRequestContext(StorageAccountScope), cancellationToken);
    }
}
