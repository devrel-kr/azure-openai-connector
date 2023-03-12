using System;

using DevRelKR.OpenAIConnector.HelperApp.Configurations;

using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Configurations.AppSettings.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

[assembly: FunctionsStartup(typeof(DevRelKR.OpenAIConnector.HelperApp.Startup))]

namespace DevRelKR.OpenAIConnector.HelperApp
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder
                   .AddEnvironmentVariables();

            base.ConfigureAppConfiguration(builder);
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            ConfigureAppSettings(builder.Services);
            ConfigureHttpClient(builder.Services);
        }

        private static void ConfigureAppSettings(IServiceCollection services)
        {
            var cogsvcSettings = services.BuildServiceProvider()
                                        .GetService<IConfiguration>()
                                        .Get<CognitiveServicesSettings>(CognitiveServicesSettings.Name);
            services.AddSingleton(cogsvcSettings);

            var options = new DefaultOpenApiConfigurationOptions()
            {
                OpenApiVersion = OpenApiVersionType.V3,
                Info = new OpenApiInfo()
                {
                    Version = "1.0.0",
                    Title = "OpenAI Connector Helper API",
                    Description = "This is an API for OpenAI Connector Helper."
                }
            };

            /* ⬇️⬇️⬇️ for GH Codespaces ⬇️⬇️⬇️ */
            var codespaces = bool.TryParse(Environment.GetEnvironmentVariable("OpenApi__RunOnCodespaces"), out var isCodespaces) && isCodespaces;
            if (codespaces)
            {
                options.IncludeRequestingHostName = false;
            }
            /* ⬆️⬆️⬆️ for GH Codespaces ⬆️⬆️⬆️ */

            services.AddSingleton<IOpenApiConfigurationOptions>(options);
        }

        private static void ConfigureHttpClient(IServiceCollection services)
        {
            services.AddHttpClient("helper");
        }
    }
}