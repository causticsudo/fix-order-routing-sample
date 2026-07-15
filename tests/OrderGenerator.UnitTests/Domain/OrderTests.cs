using OrderGenerator.Domain.Aggregates;
using OrderGenerator.Domain.ValueObjects;
using Xunit;

namespace OrderGenerator.UnitTests.Domain;

public class OrderTests
{
    [Fact]
    public void CreateOrder_WithValidInputs_CreatesOrderSuccessfully()
    {
        var symbol = Symbol.Create("PETR4");
        var side = OrderSide.Create("BUY");
        var quantity = Quantity.Create(100);
        var price = Price.Create(20.50m);

        var result = Order.Create(symbol, side, quantity, price);
        var order = (result as ResultSuccess<Order>)!.Value;

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.NotEqual(default, order.CreatedAt);
    }

    [Fact]
    public void CreateOrder_RaisesOrderCreatedEvent()
    {
        var symbol = Symbol.Create("VALE3");
        var side = OrderSide.Create("SELL");
        var quantity = Quantity.Create(50);
        var price = Price.Create(15.75m);

        var result = Order.Create(symbol, side, quantity, price);
        var order = (result as ResultSuccess<Order>)!.Value;

        var events = order.GetDomainEvents();
        Assert.Single(events);
        Assert.IsType<OrderCreatedEvent>(events[0]);
    }

    [Fact]
    public void MarkAsSubmitted_ChangesStatus()
    {
        var order = CreateTestOrder();
        order.MarkAsSubmitted();

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.NotNull(order.UpdatedAt);
    }

    [Fact]
    public void MarkAsAccepted_ChangesStatus()
    {
        var order = CreateTestOrder();
        order.MarkAsAccepted();

        Assert.Equal(OrderStatus.Accepted, order.Status);
    }

    [Fact]
    public void MarkAsRejected_ChangesStatus()
    {
        var order = CreateTestOrder();
        order.MarkAsRejected("Insufficient funds");

        Assert.Equal(OrderStatus.Rejected, order.Status);
    }

    private static Order CreateTestOrder()
    {
        var symbol = Symbol.Create("PETR4");
        var side = OrderSide.Create("BUY");
        var quantity = Quantity.Create(100);
        var price = Price.Create(20.50m);

        var result = Order.Create(symbol, side, quantity, price);
        return (result as ResultSuccess<Order>)!.Value;
    }
}
