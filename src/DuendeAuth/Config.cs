using Duende.IdentityServer.Models;
using DuendeAuth.Common.Constants;

public static class Config
{
    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope(Scopes.ScalarApi, "Scalar API")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource(Scopes.ScalarApi, "Scalar API") { Scopes = { Scopes.ScalarApi } }
    ];

    public static IEnumerable<Client> GetClients(IConfiguration configuration) =>
    [
        new Client
        {
            ClientId = ClientIds.ScalarClient,
            ClientSecrets =
            {
                new Secret(
                    (configuration[ConfigKeys.ScalarClientSecret]
                        ?? throw new InvalidOperationException($"{ConfigKeys.ScalarClientSecret} is not configured."))
                    .Sha256())
            },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { Scopes.ScalarApi },
            AllowedCorsOrigins = { CorsOrigins.ScalarApiLocal }
        }
    ];
}
