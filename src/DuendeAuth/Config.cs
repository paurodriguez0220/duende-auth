using Duende.IdentityServer.Models;

public static class Config
{
    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("scalar-api", "Scalar API")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("scalar-api", "Scalar API") { Scopes = { "scalar-api" } }
    ];

    public static IEnumerable<Client> GetClients(IConfiguration configuration) =>
    [
        new Client
        {
            ClientId = "scalar-client",
            ClientSecrets =
            {
                new Secret(
                    (configuration["Clients:ScalarClient:Secret"]
                        ?? throw new InvalidOperationException("Clients:ScalarClient:Secret is not configured."))
                    .Sha256())
            },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "scalar-api" },
            AllowedCorsOrigins = { "https://localhost:7248" }
        }
    ];
}
