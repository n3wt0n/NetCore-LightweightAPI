using IdentityServer4.AccessTokenValidation;
using LightweightApi.Common.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MVCCoreApiWithAuth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var readPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim("scope", "read").Build();

            var writePolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireClaim("scope", "write").Build();

            services.AddSingleton<IContactRepository, InMemoryContactRepository>();

            services.AddMvcCore()
                .AddDataAnnotations()
                .AddJsonFormatters();

            // set up embedded identity server
            services.AddIdentityServer()
                .AddTestClients()
                .AddTestResources()
                .AddTemporarySigningCredential();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}

            //app.UseMvc();
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            app.Map("/openid", id => {
                // use embedded identity server to issue tokens
                id.UseIdentityServer();
            });

            app.Map("/api", api => {
                // consume the JWT tokens in the API
                api.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
                {
                    Authority = "http://localhost:5000/openid",
                    RequireHttpsMetadata = false,
                });

                api.UseMvc();
            });

        }
    }
}
