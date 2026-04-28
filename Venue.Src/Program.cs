// Src/Program.cs
using Microsoft.Extensions.Logging;
using Sonyachna_Data_Forge.Infrastructure.Logging;
using Venue.Src.Domain;
using Venue.Src.Application;
using Venue.Src.Infrastructure.Data;
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
        var exampleDataPoints = new RandomWalkGenerator().Generate(DateTime.UtcNow.AddDays(-1), 1000, 150.00m);
        simulator.Run(exampleDataPoints);
    }
}