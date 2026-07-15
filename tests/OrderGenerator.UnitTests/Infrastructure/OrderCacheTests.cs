using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using OrderGenerator.Domain.Aggregates;
using OrderGenerator.Domain.ValueObjects;
using OrderGenerator.Infra.Persistence;
using Xunit;

namespace OrderGenerator.UnitTests.Infrastructure;

public class OrderCacheTests
{
    private readonly OrderCache _sut;

    public OrderCacheTests()
    {
        _sut = new OrderCache(new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public void Set_StoresOrderInCache()
    {
        var order = CreateTestOrder();

        _sut.Set(order);
        var exists = _sut.TryGet(order.Id, out var cached);

        exists.Should().Be(true);
        cached.Should().NotBeNull();
        cached!.Id.Should().Be(order.Id);
    }

    [Fact]
    public void Set_WithCustomExpiration_StoresOrderWithExpiration()
    {
        var order = CreateTestOrder();
        var expiration = TimeSpan.FromSeconds(1);

        _sut.Set(order, expiration);
        var existsImmediately = _sut.TryGet(order.Id, out _);

        existsImmediately.Should().Be(true);
    }

    [Fact]
    public void TryGet_WithNonExistentId_ReturnsFalse()
    {
        var nonExistentId = Guid.NewGuid();

        var exists = _sut.TryGet(nonExistentId, out _);

        exists.Should().Be(false);
    }

    [Fact]
    public void TryGet_WithExistingId_ReturnsTrue()
    {
        var order = CreateTestOrder();
        _sut.Set(order);

        var exists = _sut.TryGet(order.Id, out _);

        exists.Should().Be(true);
    }

    [Fact]
    public void TryGet_ReturnsCorrectOrder()
    {
        var order = CreateTestOrder();
        _sut.Set(order);

        _sut.TryGet(order.Id, out var cachedOrder);

        cachedOrder.Should().NotBeNull();
        cachedOrder!.Id.Should().Be(order.Id);
        cachedOrder.Symbol.Value.Should().Be(order.Symbol.Value);
        cachedOrder.Side.Should().Be(order.Side);
        cachedOrder.Quantity.Value.Should().Be(order.Quantity.Value);
        cachedOrder.Price.Value.Should().Be(order.Price.Value);
    }

    [Fact]
    public void Remove_DeletesOrderFromCache()
    {
        var order = CreateTestOrder();
        _sut.Set(order);

        _sut.Remove(order.Id);
        var exists = _sut.TryGet(order.Id, out _);

        exists.Should().Be(false);
    }

    [Fact]
    public void Remove_WithNonExistentId_DoesNotThrow()
    {
        var nonExistentId = Guid.NewGuid();

        var exception = Record.Exception(() => _sut.Remove(nonExistentId));

        exception.Should().BeNull();
    }

    [Fact]
    public void Set_OverwritesExistingOrder()
    {
        var order1 = CreateTestOrder();
        var order2 = CreateTestOrder();

        _sut.Set(order1);
        _sut.Set(order2);

        _sut.TryGet(order1.Id, out var cached1);
        _sut.TryGet(order2.Id, out var cached2);

        cached1.Should().NotBeNull();
        cached2.Should().NotBeNull();
        cached1!.Id.Should().Be(order1.Id);
        cached2!.Id.Should().Be(order2.Id);
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
