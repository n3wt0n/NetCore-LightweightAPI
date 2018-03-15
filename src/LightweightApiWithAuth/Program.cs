using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LightweightApi.Common;
using LightweightApi.Common.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System;
using IdentityServer4.AccessTokenValidation;

namespace LightweightApiWithAuth
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables().Build();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureLogging(l =>
                {
                    l.AddConfiguration(config.GetSection("Logging"));
                    l.AddConsole();
                })
                .ConfigureServices(s =>
                {
                    // set up embedded identity server
                    s.AddIdentityServer()
                        .AddTestClients()
                        .AddTestResources()
                        .AddDeveloperSigningCredential(); //.AddSigninCredentials(...);

                    s.AddRouting();

                    // set up authorization policy for the API
                    s.AddAuthorization(options =>
                    {
                        options.AddPolicy("API", policy =>
                        {
                            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                            policy.RequireAuthenticatedUser().RequireClaim("scope", "read");
                        });
                    });
                })
                .Configure(app =>
                {
                    // use embedded identity server to issue tokens
                    app.UseIdentityServer();

                    // authorize the whole API against the API policy
                    app.Use(async (context, next) =>
                    {
                        var authService = context.RequestServices.GetRequiredService<IAuthorizationService>();
                        var allowed = await authService.AuthorizeAsync(context.User, null, "API");
                        if (allowed.Succeeded)
                            await next();
                        else
                            context.Response.StatusCode = 401;
                    });

                    // define all API endpoints
                    app.UseRouter(r =>
                    {
                        var contactRepo = new InMemoryContactRepository();

                        r.MapGet("contacts", async (request, response, routeData) =>
                        {
                            var contacts = await contactRepo.GetAll();
                            await response.WriteJson(contacts);
                        });

                        r.MapGet("contacts/{id:int}", async (request, response, routeData) =>
                        {
                            var contact = await contactRepo.Get(Convert.ToInt32(routeData.Values["id"]));
                            if (contact == null)
                            {
                                response.StatusCode = 404;
                                return;
                            }

                            await response.WriteJson(contact);
                        });
                    });
                })
                .Build();

            host.Run();
        }
    }
}
