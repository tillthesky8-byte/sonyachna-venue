// Src/Indicators/Library/BollingerBands.cs
using Venue.Src.Domain;
namespace Venue.Src.Indicators.Library;
public class BollingerBands : IIndicator
{
    private readonly int _period;
    private readonly decimal _stdDevMultiplier;
    private readonly Queue<decimal> _values = new();
    private decimal _sum;
    private decimal _sumOfSquares;
    private string _sourceColumn;
    public decimal UpperBand { get; private set; }
    public decimal MiddleBand { get; private set; }
    public decimal LowerBand { get; private set; }

    public BollingerBands(int period, decimal stdDevMultiplier, string sourceColumn)
    {
        _period = period;
        _stdDevMultiplier = stdDevMultiplier;
        _sourceColumn = sourceColumn;
    }

    public void Update(ProcessedDataRow row)
    {
        decimal newValue = row[_sourceColumn];
        _values.Enqueue(newValue);
        _sum += newValue;
        _sumOfSquares += newValue * newValue;

        if (_values.Count > _period)
        {
            decimal oldValue = _values.Dequeue();
            _sum -= oldValue;
            _sumOfSquares -= oldValue * oldValue;
        }

        if (_values.Count == _period)
        {
            decimal mean = _sum / _period;
            decimal variance = (_sumOfSquares / _period) - (mean * mean);
            decimal stdDev = (decimal)Math.Sqrt((double)variance);

            UpperBand = mean + (_stdDevMultiplier * stdDev);
            MiddleBand = mean;
            LowerBand = mean - (_stdDevMultiplier * stdDev);
        }
    }

    public bool IsReady => _values.Count == _period;   
}