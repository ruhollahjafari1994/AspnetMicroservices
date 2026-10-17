using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using AspnetRunBasics.Services;
using Common.Logging;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AspnetRunBasics
{
    /// <summary>
    /// Defines the Startup type.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Describes the Startup operation.
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the Configuration value.
        /// </summary>
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        /// <summary>
        /// Describes the ConfigureServices operation.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<LoggingDelegatingHandler>();
            services.AddHttpContextAccessor();
            services.AddTransient<BearerTokenHandler>();
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options => { options.Cookie.Name = "AspnetRun.Auth"; options.ExpireTimeSpan = TimeSpan.FromMinutes(50); options.SlidingExpiration = false; })
            .AddOpenIdConnect(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = Configuration["Identity:Authority"];
                options.RequireHttpsMetadata = Configuration.GetValue<bool>("Identity:RequireHttpsMetadata", true);
                options.ClientId = Configuration["Identity:ClientId"] ?? "aspnetrun-web";
                options.ClientSecret = Configuration["Identity:ClientSecret"];
                options.ResponseType = "code";
                options.ResponseMode = "query";
                options.UsePkce = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "role";
                options.NonceCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.Scope.Add("email");
                options.Scope.Add("profile");
                options.Scope.Add("ecommerce_api");
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        var publicAuthority = Configuration["Identity:PublicAuthority"];
                        if (!string.IsNullOrEmpty(publicAuthority))
                            context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress.Replace(Configuration["Identity:Authority"].TrimEnd('/'), publicAuthority.TrimEnd('/'));
                        // Always request fresh user credentials. This prevents an existing
                        // Identity SSO cookie from bypassing the login form.
                        context.ProtocolMessage.Prompt = "login";
                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = context =>
                    {
                        Log.Warning(context.Failure, "OIDC sign-in callback failed; a fresh sign-in may be required.");
                        context.HandleResponse();
                        context.Response.Redirect("/auth-error");
                        return Task.CompletedTask;
                    }
                };
            });
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });

            services.AddHttpClient<ICatalogService, CatalogService>(c =>
                c.BaseAddress = new Uri(Configuration["ApiSettings:GatewayAddress"]))
                .AddHttpMessageHandler<BearerTokenHandler>()
                .AddHttpMessageHandler<LoggingDelegatingHandler>()
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());

            services.AddHttpClient<IBasketService, BasketService>(c =>
                c.BaseAddress = new Uri(Configuration["ApiSettings:GatewayAddress"]))
                .AddHttpMessageHandler<BearerTokenHandler>()
                .AddHttpMessageHandler<LoggingDelegatingHandler>()
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());

            services.AddHttpClient<IOrderService, OrderService>(c =>
                c.BaseAddress = new Uri(Configuration["ApiSettings:GatewayAddress"]))
                .AddHttpMessageHandler<BearerTokenHandler>()
                .AddHttpMessageHandler<LoggingDelegatingHandler>()
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());

            services.AddRazorPages(options =>
            {
                options.Conventions.AllowAnonymousToPage("/Index");
                options.Conventions.AllowAnonymousToPage("/Product");
                options.Conventions.AllowAnonymousToPage("/ProductDetail");
                options.Conventions.AllowAnonymousToPage("/Contact");
                options.Conventions.AllowAnonymousToPage("/Error");
            });

            services.AddHealthChecks()
                .AddUrlGroup(new Uri(Configuration["ApiSettings:GatewayAddress"]), "Ocelot API Gw", HealthStatus.Degraded);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// <summary>
        /// Describes the Configure operation.
        /// </summary>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/login", async context =>
                {
                    var properties = new AuthenticationProperties { RedirectUri = "/" };
                    properties.Items["prompt"] = "login";
                    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
                });
                endpoints.MapGet("/auth-error", async context =>
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync("<!doctype html><html lang=\"en\" dir=\"ltr\"><meta charset=\"utf-8\"><title>Sign-in failed</title><body style=\"font:16px sans-serif;max-width:560px;margin:10vh auto;padding:24px\"><h1>Sign-in could not be completed</h1><p>The sign-in request expired or was already used. Start a new sign-in and do not reload the callback URL.</p><p><a href=\"/login\">Try signing in again</a> · <a href=\"/\">Return to the store</a></p></body></html>");
                }).AllowAnonymous();
                endpoints.MapPost("/logout", async context => { await context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>().ValidateRequestAsync(context); await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); context.Response.Redirect("/"); });
                endpoints.MapRazorPages();
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                }).AllowAnonymous();
            });
        }

        /// <summary>
        /// Describes the GetRetryPolicy operation.
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            // In this case will wait for
            //  2 ^ 1 = 2 seconds then
            //  2 ^ 2 = 4 seconds then
            //  2 ^ 3 = 8 seconds then
            //  2 ^ 4 = 16 seconds then
            //  2 ^ 5 = 32 seconds

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, retryCount, context) =>
                    {
                        Log.Error($"Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, due to: {exception}.");
                    });
        }

        /// <summary>
        /// Describes the GetCircuitBreakerPolicy operation.
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );
        }
    }
}
