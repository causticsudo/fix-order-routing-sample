using OrderGenerator.Domain.ValueObjects;
using Xunit;

namespace OrderGenerator.UnitTests.Domain;

public class SymbolTests
{
    [Fact]
    public void CreateSymbol_WithValidSymbol_ReturnsSymbol()
    {
        var symbol = Symbol.Create("PETR4");

        Assert.NotNull(symbol);
        Assert.Equal("PETR4", symbol.Value);
    }

    [Theory]
    [InlineData("petr4")]
    [InlineData("PETR4")]
    [InlineData("Petr4")]
    public void CreateSymbol_NormalizesToUpperCase(string input)
    {
        var symbol = Symbol.Create(input);

        Assert.Equal("PETR4", symbol.Value);
    }
}

public class OrderSideTests
{
    [Fact]
    public void CreateOrderSide_WithBuy_ReturnsBuy()
    {
        var side = OrderSide.Create("BUY");

        Assert.Equal(OrderSide.Buy, side);
    }

    [Fact]
    public void CreateOrderSide_WithSell_ReturnsSell()
    {
        var side = OrderSide.Create("SELL");

        Assert.Equal(OrderSide.Sell, side);
    }

    [Theory]
    [InlineData("buy")]
    [InlineData("BUY")]
    public void CreateOrderSide_NormalizesToUpperCase(string input)
    {
        var side = OrderSide.Create(input);

        Assert.Equal(OrderSide.Buy, side);
    }

    [Fact]
    public void CreateOrderSide_WithInvalidSide_Throws()
    {
        Assert.Throws<ArgumentException>(() => OrderSide.Create("INVALID"));
    }
}

public class QuantityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(99_999)]
    public void CreateQuantity_ReturnsQuantity(long qty)
    {
        var quantity = Quantity.Create(qty);

        Assert.Equal(qty, quantity.Value);
    }
}

public class PriceTests
{
    [Theory]
    [InlineData("0.01")]
    [InlineData("1.50")]
    [InlineData("999.99")]
    public void CreatePrice_ReturnsPrice(string priceStr)
    {
        var priceDecimal = decimal.Parse(priceStr);
        var price = Price.Create(priceDecimal);

        Assert.Equal(priceDecimal, price.Value);
    }
}
