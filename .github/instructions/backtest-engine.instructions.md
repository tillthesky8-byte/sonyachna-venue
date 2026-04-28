---
description: "Use when working on the Backtest Engine components, strategies, portfolio management, broker execution, or domain models."
applyTo: "/**/*.cs"
---

# Backtest Engine Architecture and Guidelines

The Backtest Engine is responsible for executing backtests. It takes processed data points and applies trading strategies to simulate trades and generate results.

## Key Components
1. **Simulator**: The orchestrator (`ISimulator.cs`, `Simulator.cs`). Loops over processed data points, updating the portfolio, orders, strategy response, and equity.
2. **PortfolioManager**: Tracks cash, active positions, past trades, and last entry time. Updates market prices (`UpdateMarketPrices`) and handles order fills (`OnFillEvent`).
3. **Broker**: Simulates order execution. Has a list of pending orders, a slippage model, and processes orders (`ProcessOrders`) on each tick.
4. **SlippageModel**: Calculates execution prices (`GetExecutionPrice`) and maximum volume (`GetVolumeConstraint`) based on `SpreadPenalty` and `MaxVolumeParticipationRatio`.
5. **IStrategy**: Defines `Initialize(IStrategyConfig)` and `OnData(ProcessedDataRow, IPortfolioContext, IBroker)`.
6. **IIndicator**: Used within strategies via `Update()`, `Value`, and `IsReady`.
7. **TradeLogger**: Stores trade records, equity curve, max drawdown. Compiles the final report.

## Domain Models
- **Order, OrderEvent, Position, TradeRecord**: Represent the fundamental trading domain objects.
- **ProcessedDataPoint**: Contains OHLCV, spread, timestamp, and an indexer for external values.

## Implementation Guidelines
- When adding or modifying a strategy, implement `IStrategy` and define its configuration. Use indicators (`IIndicator`) internally.
- Order fills happen asynchronously to the strategy logic, handled via the `OnOrderFilled` event within the `Broker` to `PortfolioManager`/`TradeLogger`.
- Keep the `Simulator` orchestrator lightweight: it simply pipes data to the strategy and coordinates the updates.
