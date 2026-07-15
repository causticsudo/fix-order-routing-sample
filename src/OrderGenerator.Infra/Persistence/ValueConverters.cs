using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OrderGenerator.Domain.ValueObjects;

namespace OrderGenerator.Infra.Persistence;

public static class ValueConverters
{
    public static ValueConverter<Symbol, string> SymbolConverter() =>
        new(s => s.Value, v => CreateSymbol(v));

    public static ValueConverter<OrderSide, string> OrderSideConverter() =>
        new(s => s.Value, v => CreateOrderSide(v));

    public static ValueConverter<Quantity, long> QuantityConverter() =>
        new(q => q.Value, v => CreateQuantity(v));

    public static ValueConverter<Price, decimal> PriceConverter() =>
        new(p => p.Value, v => CreatePrice(v));

    private static Symbol CreateSymbol(string value) => Symbol.Create(value);

    private static OrderSide CreateOrderSide(string value) => OrderSide.Create(value);

    private static Quantity CreateQuantity(long value) => Quantity.Create(value);

    private static Price CreatePrice(decimal value) => Price.Create(value);
}
