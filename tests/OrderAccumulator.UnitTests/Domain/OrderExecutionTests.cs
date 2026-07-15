using FixOrderRouting.SharedKernel.Enums;
using OrderAccumulator.Domain.Aggregates;
using OrderAccumulator.Domain.ValueObjects;
using Xunit;

namespace OrderAccumulator.UnitTests.Domain;

public class OrderExecutionTests
{
    [Fact]
    public void CreateAccepted_WithValidData_CreatesAcceptedExecution()
    {
        var symbol = Symbol.Create("PETR4");
        var side = OrderSide.Create("BUY");
        var quantity = Quantity.Create(1000);
        var price = Price.Create(25.50m);

        var execution = OrderExecution.CreateAccepted("ord-001", symbol, side, quantity, price);

        Assert.NotEqual(Guid.Empty, execution.Id);
        Assert.Equal("ord-001", execution.ClOrdId);
        Assert.Equal(OrderExecutionStatus.Accepted, execution.Status);
        Assert.Null(execution.RejectionReason);
    }

    [Fact]
    public void CreateRejected_WithValidData_CreatesRejectedExecution()
    {
        var symbol = Symbol.Create("VALE3");
        var side = OrderSide.Create("SELL");
        var quantity = Quantity.Create(500);
        var price = Price.Create(80.00m);
        var reason = "Exposure limit exceeded";

        var execution = OrderExecution.CreateRejected("ord-002", symbol, side, quantity, price, reason);

        Assert.NotEqual(Guid.Empty, execution.Id);
        Assert.Equal("ord-002", execution.ClOrdId);
        Assert.Equal(OrderExecutionStatus.Rejected, execution.Status);
        Assert.Equal(reason, execution.RejectionReason);
    }

    [Fact]
    public void GetExecutionAmount_ReturnsCorrectAmount()
    {
        var symbol = Symbol.Create("VIIA4");
        var side = OrderSide.Create("BUY");
        var quantity = Quantity.Create(10_000);
        var price = Price.Create(10.50m);

        var execution = OrderExecution.CreateAccepted("ord-003", symbol, side, quantity, price);
        var amount = execution.GetExecutionAmount();

        Assert.Equal(105_000m, amount);
    }
}
