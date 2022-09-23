using Microsoft.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OidcServer.Endpoints
{
    public static class OdicEndpoint
    {
        public static IEndpointConventionBuilder MapOidcEndpoint(
            this IEndpointRouteBuilder endpoints,
            string pattern)
        {
            var builder = endpoints.MapPost(
                $"{pattern.TrimEnd('/')}/token",
                async (HttpContext ctx, IOpenIddictApplicationManager applicationManager) => await Exchange(ctx, applicationManager))
                .WithDisplayName("Exchange Token");

            return builder;

        }

        public static async Task<IResult> Exchange(HttpContext httpContext, IOpenIddictApplicationManager applicationManager)
        {
            var request = httpContext.GetOpenIddictServerRequest() ?? throw new Exception("Error when parsing Request");

            if (request.IsClientCredentialsGrantType())
                return await ClientCredentialsFlow(httpContext, applicationManager, request);
            if (request.IsPasswordGrantType())
                return await PasswordCredentialsFlow(httpContext, applicationManager, request);

            throw new NotImplementedException("The specified grant is not implemented.");
        }

        private static async Task<IResult> PasswordCredentialsFlow(
            HttpContext httpContext,
            IOpenIddictApplicationManager applicationManager,
            OpenIddictRequest request)
        {
            var application = await applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("The application cannot be found.");

            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

            // Use the client_id as the subject identifier.
            identity.AddClaim(Claims.Subject,
                await applicationManager.GetClientIdAsync(application),
                Destinations.AccessToken, Destinations.IdentityToken);

            identity.AddClaim(Claims.Name,
                await applicationManager.GetDisplayNameAsync(application),
                Destinations.AccessToken, Destinations.IdentityToken);

            return Results.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        static async Task<IResult> ClientCredentialsFlow(
            HttpContext httpContext,
            IOpenIddictApplicationManager applicationManager,
            OpenIddictRequest request)
        {
            var application = await applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("The application cannot be found.");

            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

            // Use the client_id as the subject identifier.
            identity.AddClaim(Claims.Subject,
                await applicationManager.GetClientIdAsync(application),
                Destinations.AccessToken, Destinations.IdentityToken);

            identity.AddClaim(Claims.Name,
                await applicationManager.GetDisplayNameAsync(application),
                Destinations.AccessToken, Destinations.IdentityToken);

            return Results.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
    }
}
