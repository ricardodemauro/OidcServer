using Microsoft.EntityFrameworkCore;
using OidcServer.Db;
using OidcServer.Endpoints;
using Serilog;

var c = new ConfigurationBuilder()
    .AddJsonFile("logging.json", optional: false)
    .Build();

var config = new LoggerConfiguration()
    .ReadFrom.Configuration(c);

Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine(msg));

Log.Logger = config.CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

var services = builder.Services;

services.AddAuthentication();
services.AddAuthorization();

services.AddDbContext<DbContext>(opts =>
{
    opts.UseInMemoryDatabase("OpenIddict");

    opts.UseOpenIddict();
});

services.AddOpenIddict()
    // Register the OpenIddict core components.
    .AddCore(opts =>
    {
        // Configure OpenIddict to use the Entity Framework Core stores and models.
        // Note: call ReplaceDefaultEntities() to replace the default entities.
        opts.UseEntityFrameworkCore()
            .UseDbContext<DbContext>();
    })
    .AddServer(options =>
    {
        // Enable the token endpoint.
        options.SetTokenEndpointUris("/connect/token");

        // Enable the client credentials flow.
        options.AllowClientCredentialsFlow();
        options.AllowPasswordFlow();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core options.
        options.UseAspNetCore()
                .EnableTokenEndpointPassthrough();

        options.DisableAccessTokenEncryption();
    })
    .AddValidation(opts =>
    {
        opts.UseLocalServer();

        opts.UseAspNetCore();
    });

var app = builder.Build();

if (builder.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseAuthentication();
app.UseAuthorization();

app.UseSerilogRequestLogging();

app.MapGet("/", () => "Hello World!");
app.MapOidcEndpoint("/connect");

var sp = app.Services;
await SeedDb.Seed(sp, builder.Configuration);

app.Run();
