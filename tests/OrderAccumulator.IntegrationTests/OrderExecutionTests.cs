using FixOrderRouting.SharedKernel.Constants;
using FixOrderRouting.SharedKernel.Enums;
using OrderAccumulator.Domain.Aggregates;
using OrderAccumulator.Domain.ValueObjects;
using Xunit;

namespace OrderAccumulator.IntegrationTests;

public class OrderExecutionTests
{
    [Fact]
    public void CreateAccepted_SetsPropertiesCorrectly()
    {
        var symbol = Symbol.Create("PETR4");
        var side = OrderSide.Create(BusinessConstants.Sides.Buy);
        var qty = Quantity.Create(1000);
        var price = Price.Create(25.50m);

        var execution = OrderExecution.CreateAccepted("CLO123", symbol, side, qty, price);

        Assert.Equal("CLO123", execution.ClOrdId);
        Assert.Equal(symbol.Value, execution.Symbol.Value);
        Assert.Equal(OrderExecutionStatus.Accepted, execution.Status);
        Assert.Null(execution.RejectionReason);
        Assert.NotEqual(Guid.Empty, execution.Id);
    }

    [Fact]
    public void CreateRejected_SetsRejectionReason()
    {
        var symbol = Symbol.Create("VALE3");
        var side = OrderSide.Create(BusinessConstants.Sides.Buy);
        var qty = Quantity.Create(5000);
        var price = Price.Create(40.00m);
        var reason = "Financial exposure would exceed limit";

        var execution = OrderExecution.CreateRejected("CLO456", symbol, side, qty, price, reason);

        Assert.Equal("CLO456", execution.ClOrdId);
        Assert.Equal(OrderExecutionStatus.Rejected, execution.Status);
        Assert.Equal(reason, execution.RejectionReason);
    }

    [Fact]
    public void AcceptedOrder_HasExecutedAtTimestamp()
    {
        var symbol = Symbol.Create("VIIA4");
        var side = OrderSide.Create(BusinessConstants.Sides.Sell);
        var qty = Quantity.Create(2000);
        var price = Price.Create(15.75m);

        var before = DateTime.UtcNow;
        var execution = OrderExecution.CreateAccepted("CLO789", symbol, side, qty, price);
        var after = DateTime.UtcNow;

        Assert.True(execution.ExecutedAt >= before && execution.ExecutedAt <= after);
    }

    [Fact]
    public void SideCanBeDetermined()
    {
        var buySide = OrderSide.Create(BusinessConstants.Sides.Buy);
        var sellSide = OrderSide.Create(BusinessConstants.Sides.Sell);

        Assert.True(buySide.IsBuy());
        Assert.True(sellSide.IsSell());
        Assert.False(buySide.IsSell());
        Assert.False(sellSide.IsBuy());
    }
}
