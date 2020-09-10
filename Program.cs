using System;
using Yandex.Cloud.Endpoint;
using Yandex.Cloud.Resourcemanager.V1;
using Yandex.Cloud;
using Yandex.Cloud.Credentials;
using Microsoft.Extensions.Configuration;
using System.IO;
using Serilog;

namespace VmManager
{
    public static class Program
    {

        private static IConfiguration configuration;
        public static void Main(string[] args)
        {
            initConfig();
            initLogger();
            var sdk = new Sdk(new OAuthCredentialsProvider(configuration.GetSection("Yandex")["oAuth"]));
            var response = sdk.Services.Resourcemanager.CloudService.List(new ListCloudsRequest());

            foreach (var c in response.Clouds)
            {
                Log.Information($"* {c.Name} ({c.Id})");
            }
        }

        private static void initConfig()
        {
            string EnvName = Environment.GetEnvironmentVariable("ENVIRONMENT", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(EnvName))
                EnvName = "Development";

            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile($"appsettings.{EnvName}.json", optional: true);

            configuration = builder.Build();
        }
        private static void initLogger()
        {
             Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
        }
    }
}
