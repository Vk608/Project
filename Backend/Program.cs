using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml;
using FABBatchValidator.Configuration;
using FABBatchValidator.Excel;
using FABBatchValidator.Agent;
using FABBatchValidator.Services;
using FABBatchValidator.Storage;
using FABBatchValidator.QueryBuilder;
using FABBatchValidator.ResponseParsing;

namespace FABBatchValidator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Set EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                Console.WriteLine("=== FAB Batch Validator Web API ===\n");

                // Build the web host
                var builder = WebApplication.CreateBuilder(args);

                // Load configuration from config.json
                Console.WriteLine("[Main] Loading configuration...");
                var pipelineConfig = ConfigurationLoader.LoadFromFile("config.json");
                Console.WriteLine("[Main] Configuration loaded successfully.\n");

                // Register services in dependency injection container
                Console.WriteLine("[Main] Registering services...");

                // Register singleton configuration
                builder.Services.AddSingleton(pipelineConfig);

                // Register pipeline components
                builder.Services.AddSingleton(new ExcelInputReader(
                    pipelineConfig.DataProcessing.FileHandling,
                    pipelineConfig.DataProcessing.InputSchema));

                builder.Services.AddSingleton<QueryTemplateBuilder>();

                builder.Services.AddHttpClient();
                builder.Services.AddSingleton(new AgentApiClient(pipelineConfig.AgentApi));

                builder.Services.AddSingleton<ResponseParser>();

                // Register validation service
                builder.Services.AddScoped<ValidationService>();

                // Register JSON result repository
                builder.Services.AddSingleton(new JsonResultRepository(
                    pipelineConfig.DataProcessing.FileHandling.ResultsJsonPath ?? "results.json"));

                // Register background job manager for async validation
                builder.Services.AddSingleton<BackgroundJobManager>();

                // Add controllers and Swagger
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                // Add CORS policy
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowLocalhost4200", policy =>
                    {
                        policy.WithOrigins("http://localhost:4200")
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });

                Console.WriteLine("[Main] All services registered.\n");

                // Build the app
                var app = builder.Build();

                // Configure middleware
                if (app.Environment.EnvironmentName == "Development")
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                // Enable CORS (must be before UseHttpsRedirection and MapControllers)
                app.UseCors("AllowLocalhost4200");

               

                app.UseAuthorization();
                app.MapControllers();

                // Start the server
                Console.WriteLine("[Main] Starting web server...\n");
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Main] FATAL ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}
