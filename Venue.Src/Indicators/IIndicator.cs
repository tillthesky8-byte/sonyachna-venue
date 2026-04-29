// Src/Indicators/IIndicator.cs
using Venue.Src.Domain;
namespace Venue.Src.Indicators;
public interface IIndicator
{
    void Update(ProcessedDataRow row);
    bool IsReady { get; }
}