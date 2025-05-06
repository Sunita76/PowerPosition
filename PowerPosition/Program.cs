using PowerPosition;
using Serilog;

var logFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
Log.Logger = new LoggerConfiguration()
               .WriteTo.Console()
               .WriteTo.File(
                   Path.Combine(logFolder, "power-position-worker.log"),
                   rollingInterval: RollingInterval.Day,
                   outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message}{NewLine}")
                  .CreateLogger();
try
{
    // Build and run the Host
    await Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            config.SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                  .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                  .AddCommandLine(args); // allow overriding with command-line args
        })
        .ConfigureServices((context, services) =>
        {            
            services.AddHostedService<PowerPositionWorker>();
        })
        .UseSerilog()
        .Build()
        .RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}



