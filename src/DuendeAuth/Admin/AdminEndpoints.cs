using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using DuendeAuth.Admin.Models;
using DuendeAuth.Common.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DuendeAuth.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/v1/users")
            .RequireAuthorization(PolicyNames.AdminRead)
            .RequireRateLimiting(PolicyNames.RateLimit)
            .WithTags("Users")
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(429);

        users.MapGet("/", GetUsersAsync)
            .WithSummary("List all users")
            .Produces<CursorResult<UserDto>>();

        users.MapPost("/", CreateUserAsync)
            .WithSummary("Create a user")
            .RequireAuthorization(PolicyNames.Admin)
            .Produces<UserDto>(201)
            .ProducesProblem(400);

        users.MapDelete("/{id}", DeleteUserAsync)
            .WithSummary("Delete a user")
            .RequireAuthorization(PolicyNames.Admin)
            .Produces(204)
            .ProducesProblem(404);

        users.MapGet("/{id}/claims", GetUserClaimsAsync)
            .WithSummary("List a user's claims")
            .Produces<List<ClaimDto>>()
            .ProducesProblem(404);

        users.MapPost("/{id}/claims", AddUserClaimAsync)
            .WithSummary("Add a claim to a user")
            .RequireAuthorization(PolicyNames.Admin)
            .Produces<ClaimDto>(201)
            .ProducesProblem(400)
            .ProducesProblem(404);

        users.MapDelete("/{id}/claims/{type}", RemoveUserClaimAsync)
            .WithSummary("Remove a claim from a user")
            .RequireAuthorization(PolicyNames.Admin)
            .Produces(204)
            .ProducesProblem(404);

        var clients = app.MapGroup("/api/v1/clients")
            .RequireAuthorization(PolicyNames.AdminRead)
            .RequireRateLimiting(PolicyNames.RateLimit)
            .WithTags("Clients")
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(429);

        clients.MapGet("/", GetClientsAsync)
            .WithSummary("List registered clients")
            .Produces<List<ClientSummaryDto>>();

        clients.MapPost("/", CreateClientAsync)
            .WithSummary("Register a new client")
            .RequireAuthorization(PolicyNames.Admin)
            .Produces<ClientSummaryDto>(201)
            .ProducesProblem(400)
            .ProducesProblem(409);

        clients.MapDelete("/{clientId}", DeleteClientAsync)
            .WithSummary("Delete a client")
            .RequireAuthorization(PolicyNames.Admin)
            .Produces(204)
            .ProducesProblem(404)
            .ProducesProblem(409);

        return app;
    }

    private static async Task<IResult> GetUsersAsync(
        UserManager<IdentityUser> userManager,
        int limit = 50,
        string? cursor = null,
        CancellationToken ct = default)
    {
        const int maxLimit = 100;
        limit = Math.Clamp(limit, 1, maxLimit);

        string? decodedCursor = null;
        if (cursor is not null)
        {
            try { decodedCursor = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)); }
            catch { return Results.ValidationProblem(new Dictionary<string, string[]> { ["cursor"] = ["Invalid cursor value."] }); }
        }

        var query = userManager.Users.AsQueryable();
        if (decodedCursor is not null)
            query = query.Where(u => u.UserName != null && u.UserName.CompareTo(decodedCursor) > 0);

        var items = await query
            .OrderBy(u => u.UserName)
            .Take(limit + 1)
            .Select(u => new UserDto(u.Id, u.UserName!, u.Email, u.EmailConfirmed))
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore) items.RemoveAt(items.Count - 1);

        var nextCursor = hasMore
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes(items[^1].UserName))
            : null;

        return Results.Ok(new CursorResult<UserDto>(items, new CursorPaginationMeta(nextCursor, hasMore)));
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
            return Results.ValidationProblem(
                validationResults
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Invalid value.").ToArray()));

        var user = new IdentityUser { UserName = request.UserName, Email = request.Email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return Results.ValidationProblem(
                result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

        return Results.Created($"/api/v1/users/{user.Id}",
            new UserDto(user.Id, user.UserName!, user.Email, user.EmailConfirmed));
    }

    private static async Task<IResult> DeleteUserAsync(
        string id,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.NotFound();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return Results.Problem(title: "Failed to delete user.", statusCode: StatusCodes.Status500InternalServerError);

        return Results.NoContent();
    }

    private static async Task<IResult> GetUserClaimsAsync(
        string id,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.NotFound();

        var claims = await userManager.GetClaimsAsync(user);
        return Results.Ok(claims.Select(c => new ClaimDto(c.Type, c.Value)));
    }

    private static async Task<IResult> AddUserClaimAsync(
        string id,
        ClaimDto request,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
            return Results.ValidationProblem(
                validationResults
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Invalid value.").ToArray()));

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.NotFound();

        var result = await userManager.AddClaimAsync(user, new Claim(request.Type, request.Value));

        if (!result.Succeeded)
            return Results.ValidationProblem(
                result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

        return Results.Created($"/api/v1/users/{id}/claims", request);
    }

    private static async Task<IResult> RemoveUserClaimAsync(
        string id,
        string type,
        UserManager<IdentityUser> userManager,
        CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return Results.NotFound();

        var claims = await userManager.GetClaimsAsync(user);
        var claim = claims.FirstOrDefault(c => c.Type == type);
        if (claim is null) return Results.NotFound();

        await userManager.RemoveClaimAsync(user, claim);
        return Results.NoContent();
    }

    private static async Task<IResult> GetClientsAsync(
        ConfigurationDbContext configDb,
        CancellationToken ct)
    {
        var clients = await configDb.Clients
            .Include(c => c.AllowedScopes)
            .Include(c => c.AllowedGrantTypes)
            .ToListAsync(ct)
            ;

        return Results.Ok(clients.Select(c => new ClientSummaryDto(
            c.ClientId,
            c.AllowedScopes.Select(s => s.Scope).ToList(),
            c.AllowedGrantTypes.Select(g => g.GrantType).ToList())));
    }

    private static async Task<IResult> CreateClientAsync(
        CreateClientRequest request,
        ConfigurationDbContext configDb,
        CancellationToken ct)
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
            return Results.ValidationProblem(
                validationResults
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Invalid value.").ToArray()));

        if (await configDb.Clients.AnyAsync(c => c.ClientId == request.ClientId, ct))
            return Results.Problem(title: "A client with this ID already exists.", statusCode: 409);

        var isCodeFlow = string.Equals(request.GrantType, GrantTypeNames.AuthorizationCode, StringComparison.OrdinalIgnoreCase);

        if (isCodeFlow && (request.RedirectUris is null || request.RedirectUris.Count == 0))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.RedirectUris)] = ["RedirectUris is required for authorization_code clients."]
            });

        var grantTypes = isCodeFlow ? GrantTypes.Code : GrantTypes.ClientCredentials;
        var grantTypeNames = isCodeFlow
            ? (IReadOnlyList<string>)[GrantTypeNames.AuthorizationCode]
            : [GrantTypeNames.ClientCredentials];

        var client = new Client
        {
            ClientId = request.ClientId,
            ClientSecrets = { new Secret(request.Secret.Sha256()) },
            AllowedGrantTypes = grantTypes,
            AllowedScopes = request.AllowedScopes,
            AllowedCorsOrigins = request.AllowedCorsOrigins ?? [],
            RedirectUris = request.RedirectUris ?? [],
            PostLogoutRedirectUris = request.PostLogoutRedirectUris ?? []
        }.ToEntity();

        configDb.Clients.Add(client);
        await configDb.SaveChangesAsync(ct);

        return Results.Created(
            $"/api/v1/clients/{request.ClientId}",
            new ClientSummaryDto(request.ClientId, request.AllowedScopes, grantTypeNames));
    }

    private static async Task<IResult> DeleteClientAsync(
        string clientId,
        ConfigurationDbContext configDb,
        CancellationToken ct)
    {
        if (clientId == ClientIds.AdminClient)
            return Results.Problem(title: "The admin client cannot be deleted.", statusCode: 409);

        var client = await configDb.Clients
            .FirstOrDefaultAsync(c => c.ClientId == clientId, ct)
            ;

        if (client is null) return Results.NotFound();

        configDb.Clients.Remove(client);
        await configDb.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
