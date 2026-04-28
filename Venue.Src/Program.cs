// Src/Program.cs
using Microsoft.Extensions.Logging;
using Sonyachna_Data_Forge.Infrastructure.Logging;
using Venue.Src.Domain;
using Venue.Src.Application;
namespace Venue.Src;
class Program
{
    static void Main(string[] args)
    {
        using var loggerFactory = LoggerFactoryProvider.Create();
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("Venue backtest engine started.");


        var simulatorLogger = loggerFactory.CreateLogger<Simulator>();
        var simulator = new Simulator(simulatorLogger);
        var exampleDataPoints = new List<ProcessedDataRow>
        {
            new ProcessedDataRow { Timestamp = DateTime.UtcNow, Open = 150.00m, High = 151.00m, Low = 149.50m, Close = 150.50m, Volume = 1000, Spread = 0.50m, Externals = new Dictionary<string, decimal> { { "us_interest_rate", 100 } } },
            new ProcessedDataRow { Timestamp = DateTime.UtcNow.AddSeconds(1), Open = 150.50m, High = 151.50m, Low = 149.50m, Close = 151.00m, Volume = 500, Spread = 0.50m, Externals = new Dictionary<string, decimal> { { "us_interest_rate", 100 } } },
        };
        simulator.Run(exampleDataPoints);
    }
}