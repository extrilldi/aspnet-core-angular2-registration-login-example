using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WebApi.Helpers;
using WebApi.Services;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System;

namespace WebApi
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            // InMemory db needs to be named in 2.0
            services.AddDbContext<DataContext>(options => options.UseInMemoryDatabase("base"));
            services.AddMvc();
            services.AddAutoMapper();

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            // as far as I understand, a service locator. Ewww.
            var serviceProvider = services.BuildServiceProvider();
            var appSettings = serviceProvider.GetService<IOptions<AppSettings>>().Value;
            
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                options.Authority = appSettings.ServerSiteUrl;
                options.Audience = appSettings.ClientSiteUrl;
                options.TokenValidationParameters = new TokenValidationParameters() {                
                    // Checking the key.
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Secret)),
                     
                    // Validate JWT Audience (aud) claim
                    ValidateAudience = true,
                    ValidAudience = appSettings.ServerSiteUrl,

                    // Validate JWT Issuer (iss) claim
                    ValidateIssuer = true,
                    ValidIssuer = appSettings.ClientSiteUrl,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });
            

            // configure DI for application services
            services.AddScoped<IUserService, UserService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
