// Src/Indicators/Library/SMA.cs
using Venue.Src.Domain;
namespace Venue.Src.Indicators.Library;
public class SMA : IIndicator
{
    private readonly int _period;
    private readonly string _source;
    private readonly Queue<decimal> _window = new Queue<decimal>();
    private decimal _sum;
    public decimal Value {get; private set;}
    public bool IsReady => _window.Count >= _period;

    public SMA(int period, string source)
    {
        _period = period;
        _source = source;
    }

    public void Update(ProcessedDataRow row)
    {
        decimal value = row[_source];
        
        _window.Enqueue(value);
        _sum += value;

        if (_window.Count > _period)
            _sum -= _window.Dequeue();
        

        Value = _sum / Math.Min(_window.Count, _period);
    }
    
}