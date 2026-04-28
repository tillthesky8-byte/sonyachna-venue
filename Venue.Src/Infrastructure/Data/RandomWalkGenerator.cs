// Src/Infrastructure/Data/RandomWalkGenerator.cs
using System;
using Venue.Src.Domain;
namespace Venue.Src.Infrastructure.Data;
public class RandomWalkGenerator()
{
    private readonly Random _random = new(); 
    public IEnumerable<ProcessedDataRow> Generate(DateTime startTime, int count, decimal initialPrice)
    {
        var currentPrice = initialPrice;
        for (int i = 0; i < count; i++)
        {
            var changePercent = (decimal)(_random.NextDouble() - 0.5) * 0.02m; 
            currentPrice += currentPrice * changePercent;

            yield return new ProcessedDataRow
            {
                Timestamp = startTime.AddHours(i),
                Open = currentPrice,
                High = currentPrice * (1 + (decimal)_random.NextDouble() * 0.01m),
                Low = currentPrice * (1 - (decimal)_random.NextDouble() * 0.01m),
                Close = currentPrice,
                Spread = 0.01m,
                Volume = _random.Next(100, 1000),
                Externals = new Dictionary<string, decimal> { { "us_interest_rate", 100 } }

            };
        }
    }   
}