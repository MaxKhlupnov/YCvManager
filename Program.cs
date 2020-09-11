using System;
using Microsoft.Extensions.Configuration;
using System.IO;
using Serilog;
using VmManager.StateMachine;

namespace VmManager
{
    public static class Program
    {

        private static IConfiguration configuration;
        public static void Main(string[] args)
        {
            initConfig();
            initLogger();
            VmState state = new StartingVmState(VmIstanceState.Unspecified);
            Context context = new Context(configuration, state);
                while(state != null)
                {
                    state =  context.Request().GetAwaiter().GetResult();
                };
            Log.Information("Workflow compleated");
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
