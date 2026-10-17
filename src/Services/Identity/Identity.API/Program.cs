using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("IdentityConnectionString")
    ?? throw new InvalidOperationException("ConnectionStrings:IdentityConnectionString is required.");
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.UseOpenIddict();
});
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
})
.AddEntityFrameworkStores<IdentityDbContext>().AddDefaultTokenProviders();
builder.Services.AddAntiforgery(options => options.FormFieldName = "__RequestVerificationToken");
builder.Services.AddAuthorization();
builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>())
    .AddServer(options =>
    {
        var issuer = new Uri(builder.Configuration["Identity:Issuer"] ?? "http://identity.api/");
        if (!builder.Environment.IsDevelopment() && issuer.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Identity:Issuer must use HTTPS outside Development.");
        options.SetIssuer(issuer);
        options.SetAuthorizationEndpointUris("connect/authorize");
        options.SetTokenEndpointUris("connect/token");
        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
        options.AllowClientCredentialsFlow();
        options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Profile, "ecommerce_api");
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        }
        else
        {
            var certificatePath = builder.Configuration["Identity:Certificates:Path"] ?? throw new InvalidOperationException("Identity:Certificates:Path is required outside Development.");
            var certificatePassword = builder.Configuration["Identity:Certificates:Password"];
            var certificate = new X509Certificate2(certificatePath, certificatePassword);
            options.AddSigningCertificate(certificate).AddEncryptionCertificate(certificate);
        }
        options.DisableAccessTokenEncryption();
        var aspNetCore = options.UseAspNetCore();
        if (builder.Environment.IsDevelopment()) aspNetCore.DisableTransportSecurityRequirement();
        aspNetCore.EnableAuthorizationEndpointPassthrough();
    });

var app = builder.Build();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Creates the initial schema and registers OAuth clients and an optional administrator.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try { await db.Database.EnsureCreatedAsync(); break; }
        catch (Exception ex) when (attempt < 30)
        {
            app.Logger.LogWarning(ex, "Waiting for the Identity SQL database (attempt {Attempt}/30).", attempt);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Customer", "Administrator" })
        if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));

    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var webClientId = app.Configuration["Identity:WebClientId"] ?? "aspnetrun-web";
    if (await manager.FindByClientIdAsync(webClientId) is null)
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = webClientId,
            ClientSecret = app.Configuration["Identity:WebClientSecret"] ?? throw new InvalidOperationException("Identity:WebClientSecret is required."),
            DisplayName = "AspnetRun Web",
            ConsentType = ConsentTypes.Implicit,
            RedirectUris = { new Uri(app.Configuration["Identity:WebRedirectUri"] ?? "http://localhost:8006/signin-oidc") },
            PostLogoutRedirectUris = { new Uri(app.Configuration["Identity:WebLogoutRedirectUri"] ?? "http://localhost:8006/signout-callback-oidc") },
            Permissions =
            {
                Permissions.Endpoints.Authorization, Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode, Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email, Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + "ecommerce_api"
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        });
    var serviceId = app.Configuration["Identity:BasketClientId"] ?? "basket-service";
    if (await manager.FindByClientIdAsync(serviceId) is null)
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = serviceId,
            ClientSecret = app.Configuration["Identity:BasketClientSecret"] ?? throw new InvalidOperationException("Identity:BasketClientSecret is required."),
            DisplayName = "Basket service",
            Permissions =
            {
                Permissions.Endpoints.Token, Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + "ecommerce_api"
            }
        });

    var adminName = app.Configuration["Identity:BootstrapAdmin:UserName"];
    var adminPassword = app.Configuration["Identity:BootstrapAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminName) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await users.FindByNameAsync(adminName);
        if (admin is null)
        {
            admin = new IdentityUser { UserName = adminName, Email = app.Configuration["Identity:BootstrapAdmin:Email"] ?? $"{adminName}@localhost", EmailConfirmed = true };
            var result = await users.CreateAsync(admin, adminPassword);
            if (result.Succeeded) await users.AddToRoleAsync(admin, "Administrator");
            else app.Logger.LogError("Bootstrap administrator was not created: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}

app.MapGet("/account/login", (HttpContext context, Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, string? returnUrl, string? error) =>
{
    var safeReturnUrl = System.Net.WebUtility.HtmlEncode(returnUrl ?? "/");
    var registerUrl = Uri.EscapeDataString(returnUrl ?? "/");
    var csrf = System.Net.WebUtility.HtmlEncode(antiforgery.GetAndStoreTokens(context).RequestToken);
    var errorMessage = error == "invalid" ? "<p role=\"alert\" style=\"color:#b91c1c\">Invalid username or password. Please try again.</p>" : "";
    var html = "<!doctype html><html lang=\"en\" dir=\"ltr\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Sign in</title><style>body{font:16px sans-serif;max-width:420px;margin:10vh auto;padding:24px;background:#f4f6f8;color:#172554}form{background:white;padding:28px;border-radius:12px;box-shadow:0 12px 36px #17255418}input,button{box-sizing:border-box;width:100%;padding:12px;margin:8px 0}button{background:#172554;color:white;border:0;border-radius:6px;cursor:pointer}a{color:#1d4ed8}</style><form method=\"post\" action=\"/account/login\"><h1>Sign in</h1>" + errorMessage + "<label for=\"userName\">Username or email</label><input id=\"userName\" name=\"userName\" autocomplete=\"username\" required/><label for=\"password\">Password</label><input id=\"password\" name=\"password\" type=\"password\" autocomplete=\"current-password\" required/><input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"" + csrf + "\"/><input name=\"returnUrl\" type=\"hidden\" value=\"" + safeReturnUrl + "\"/><button type=\"submit\">Sign in</button><p><a href=\"/account/register?returnUrl=" + registerUrl + "\">Create an account</a></p></form></html>";
    return Results.Content(html, "text/html; charset=utf-8");
});
app.MapPost("/account/login", async (HttpRequest request, Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users) =>
{
    try { await antiforgery.ValidateRequestAsync(request.HttpContext); } catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException) { return Results.BadRequest(); }
    var form = await request.ReadFormAsync();
    var login = form["userName"].ToString();
    var user = await users.FindByNameAsync(login) ?? await users.FindByEmailAsync(login);
    if (user is null) return Results.Redirect("/account/login?error=invalid");
    // Keep the authentication cookie for this browser session only. A fresh browser session
    // must not silently reuse a remembered development account.
    var result = await signIn.PasswordSignInAsync(user, form["password"].ToString(), false, true);
    if (!result.Succeeded) return Results.Redirect("/account/login?error=invalid");
    var returnUrl = form["returnUrl"].ToString();
    return Results.LocalRedirect(UrlIsLocal(returnUrl) ? returnUrl : "/");
});
app.MapGet("/account/register", (HttpContext context, Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, string? returnUrl) =>
{
    var safeReturnUrl = System.Net.WebUtility.HtmlEncode(returnUrl ?? "/");
    var loginUrl = Uri.EscapeDataString(returnUrl ?? "/");
    var csrf = System.Net.WebUtility.HtmlEncode(antiforgery.GetAndStoreTokens(context).RequestToken);
    var html = "<!doctype html><html lang=\"en\" dir=\"ltr\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Create an account</title><style>body{font:16px sans-serif;max-width:420px;margin:10vh auto;padding:24px;background:#f4f6f8;color:#172554}form{background:white;padding:28px;border-radius:12px;box-shadow:0 12px 36px #17255418}input,button{box-sizing:border-box;width:100%;padding:12px;margin:8px 0}button{background:#172554;color:white;border:0;border-radius:6px;cursor:pointer}a{color:#1d4ed8}</style><form method=\"post\" action=\"/account/register\"><h1>Create an account</h1><label for=\"userName\">Username</label><input id=\"userName\" name=\"userName\" autocomplete=\"username\" required/><label for=\"email\">Email address</label><input id=\"email\" name=\"email\" type=\"email\" autocomplete=\"email\" required/><label for=\"password\">Password (at least 8 characters, including an uppercase letter and a number)</label><input id=\"password\" name=\"password\" type=\"password\" autocomplete=\"new-password\" required/><input name=\"__RequestVerificationToken\" type=\"hidden\" value=\"" + csrf + "\"/><input name=\"returnUrl\" type=\"hidden\" value=\"" + safeReturnUrl + "\"/><button type=\"submit\">Create account</button><p><a href=\"/account/login?returnUrl=" + loginUrl + "\">Already have an account? Sign in</a></p></form></html>";
    return Results.Content(html, "text/html; charset=utf-8");
});
app.MapPost("/account/register", async (HttpRequest request, Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery, UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn) =>
{
    try { await antiforgery.ValidateRequestAsync(request.HttpContext); } catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException) { return Results.BadRequest(); }
    var form = await request.ReadFormAsync();
    var user = new IdentityUser { UserName = form["userName"].ToString(), Email = form["email"].ToString() };
    var result = await users.CreateAsync(user, form["password"].ToString());
    if (!result.Succeeded) return Results.Content("Registration failed: " + string.Join("; ", result.Errors.Select(e => e.Description)), "text/plain", statusCode: 400);
    await users.AddToRoleAsync(user, "Customer");
    await signIn.SignInAsync(user, false);
    var returnUrl = form["returnUrl"].ToString();
    return Results.LocalRedirect(UrlIsLocal(returnUrl) ? returnUrl : "/");
});
app.MapMethods("/connect/authorize", new[] { "GET", "POST" }, async (HttpContext context, UserManager<IdentityUser> users) =>
{
    var request = Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers.GetOpenIddictServerRequest(context) ?? throw new InvalidOperationException("OpenID Connect request is missing.");
    if (context.Request.Query["prompt"].ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("login", StringComparer.Ordinal))
    {
        var query = QueryString.Create(context.Request.Query
            .Where(parameter => !string.Equals(parameter.Key, "prompt", StringComparison.OrdinalIgnoreCase))
            .SelectMany(parameter => parameter.Value.Select(value => new KeyValuePair<string, string?>(parameter.Key, value))));
        var returnUrl = "/connect/authorize" + query;
        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
        return Results.Redirect("/account/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
    }
    if (context.User.Identity?.IsAuthenticated != true)
    {
        var returnUrl = "/connect/authorize" + context.Request.QueryString;
        return Results.Redirect("/account/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
    }
    var user = await users.GetUserAsync(context.User);
    if (user is null) return Results.Unauthorized();
    var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
    identity.SetClaim(Claims.Subject, await users.GetUserIdAsync(user));
    identity.SetClaim(Claims.Name, await users.GetUserNameAsync(user));
    identity.SetClaim(Claims.Email, await users.GetEmailAsync(user));
    foreach (var role in await users.GetRolesAsync(user)) identity.AddClaim(Claims.Role, role);
    identity.SetScopes(request.GetScopes());
    identity.SetResources("ecommerce_api");
    identity.SetDestinations(claim => claim.Type switch
    {
        Claims.Name or Claims.Email or Claims.Role or Claims.Subject => new[] { Destinations.AccessToken, Destinations.IdentityToken },
        _ => new[] { Destinations.AccessToken }
    });
    return Results.SignIn(new ClaimsPrincipal(identity), properties: null, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.Run();

static bool UrlIsLocal(string value) => !string.IsNullOrEmpty(value) && value.StartsWith('/') && !value.StartsWith("//");

/// <summary>
/// Defines the IdentityDbContext type.
/// </summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
    /// <summary>
    /// Describes the OnModelCreating operation.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder) { base.OnModelCreating(builder); builder.UseOpenIddict(); }
}
