// Src/Program.cs
using Microsoft.Extensions.Logging;
using Sonyachna_Data_Forge.Infrastructure.Logging;
using Venue.Src.Domain;
namespace Venue.Src;
class Program
{
    static void Main(string[] args)
    {
        using var loggerFactory = LoggerFactoryProvider.Create();
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("Venue backtest engine started.");
    }
}