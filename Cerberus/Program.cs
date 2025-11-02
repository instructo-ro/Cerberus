
using Cerberus.Application;
using Cerberus.Infrastructure;
using Cerberus.Surface;
using Scalar.AspNetCore;

namespace Cerberus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();


            // Configure JSON serialization - serialize enums as strings
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Cerberus API",
                    Version = "v1",
                    Description = "A secure API-based secrets management system"
                });

                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "API Key",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter your Cerberus API key (format: cerb_xxxxxxxxxxxxx)"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") =="Development")
            {
                builder.Services.AddCors(options =>
                    options.AddPolicy("AllowAll", (builder)=>builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin())
                );
            }

            // Database
            var dbName = Environment.GetEnvironmentVariable("DATABASE_NAME");
            var dbUser = Environment.GetEnvironmentVariable("DATABASE_USER");
            var dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
            var dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST");
            var dbPort = Environment.GetEnvironmentVariable("DATABASE_PORT");
            var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

            builder.Services.AddSingleton<IDbConnectionFactory>(new PostgresConnectionFactory(connectionString));

            // Repositories
            builder.Services.AddScoped<TenantRepository>();
            builder.Services.AddScoped<ApiKeyRepository>();

            // Services
            builder.Services.AddScoped<TenantService>();
            builder.Services.AddScoped<ApiKeyService>();

            var app = builder.Build();
        
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger(options =>
                {
                    options.RouteTemplate="cerberus/swagger/{documentName}/swagger.json";
                });
                app.MapScalarApiReference("/cerberus/scalar", options =>
                {
                    options
                        .WithDynamicBaseServerUrl()
                        .WithTitle("Cerberus API Documentation")
                        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                        .WithOpenApiRoutePattern("/cerberus/swagger/{documentName}/swagger.json");
                });
                app.UseCors("AllowAll");

            }
            app.UseHttpsRedirection();

            // API Key Authentication Middleware
            app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

           // app.UseAuthorization();

            // Register endpoints
            app.MapBootstrapEndpoints(); // Bootstrap: Create first tenant + API key
            app.MapApiKeyEndpoints(); // API key management
            app.MapTenantEndpoints();
            app.MapProjectEndpoints();
            app.MapAnimaEndpoints();

            app.Run();
        }
    }
}
