using FixOrderRouting.SharedKernel.Constants;
using FixOrderRouting.SharedKernel.Enums;
using OrderAccumulator.Domain.Abstractions;
using OrderAccumulator.Domain.ValueObjects;

namespace OrderAccumulator.Domain.Services;

public class ExposureCalculator
{
    private readonly IOrderExecutionRepository _repository;

    public ExposureCalculator(IOrderExecutionRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> CanAcceptOrderAsync(Symbol symbol, OrderSide side, Quantity quantity, Price price, CancellationToken cancellationToken = default)
    {
        var currentExposure = await GetExposureAsync(symbol.Value, cancellationToken);
        var orderAmount = price.Value * quantity.Value;

        var newExposure = side.IsBuy()
            ? currentExposure + orderAmount
            : currentExposure - orderAmount;

        return Math.Abs(newExposure) <= BusinessConstants.Orders.ExposureLimit;
    }

    public async Task<decimal> GetExposureAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var orders = await _repository.GetBySymbolAsync(symbol, cancellationToken);
        var acceptedOrders = orders.Where(o => o.Status == OrderExecutionStatus.Accepted).ToList();

        var buy = acceptedOrders
            .Where(o => o.Side.IsBuy())
            .Sum(o => o.Price.Value * o.Quantity.Value);

        var sell = acceptedOrders
            .Where(o => o.Side.IsSell())
            .Sum(o => o.Price.Value * o.Quantity.Value);

        return buy - sell;
    }

    public async Task<Dictionary<string, decimal>> GetAllExposuresAsync(CancellationToken cancellationToken = default)
    {
        var allOrders = await _repository.GetAllAsync(cancellationToken);
        var symbols = allOrders.Select(o => o.Symbol.Value).Distinct();

        var exposures = new Dictionary<string, decimal>();
        foreach (var symbol in symbols)
        {
            exposures[symbol] = await GetExposureAsync(symbol, cancellationToken);
        }

        return exposures;
    }
}
