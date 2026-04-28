// Src/Application/Simulator.cs
using Venue.Src.Domain;
using Microsoft.Extensions.Logging;
namespace Venue.Src.Application;
public class Simulator(ILogger<Simulator> logger) : ISimulator
{
    private readonly ILogger<Simulator> _logger = logger;
    public void Run(IEnumerable<ProcessedDataRow> dataStream)
    {
        _logger.LogInformation("Simulator started with {Count} data points.", dataStream.Count());
        foreach (var tick in dataStream)
        {
            _logger.LogDebug("Processing tick at {Timestamp}: O={Open}, H={High}, L={Low}, C={Close}, V={Volume}, Spread={Spread}",
                tick.Timestamp, tick.Open, tick.High, tick.Low, tick.Close, tick.Volume, tick.Spread);
        }
        _logger.LogInformation("Simulator finished processing data stream.");
    }
}
