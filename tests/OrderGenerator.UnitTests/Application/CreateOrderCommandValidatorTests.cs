using AwesomeAssertions;
using OrderGenerator.Application.Features.Orders.CreateOrder;
using Xunit;

namespace OrderGenerator.UnitTests.Application;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ReturnsNoErrors()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(true);
        result.Errors.Should().HaveCount(0);
    }

    [Fact]
    public async Task Validate_WithEmptySymbol_ReturnsError()
    {
        var command = new CreateOrderCommand("", "BUY", 100, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Symbol is required")).Should().Be(true);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("XYZ")]
    [InlineData("UNKNOWN")]
    public async Task Validate_WithInvalidSymbol_ReturnsError(string symbol)
    {
        var command = new CreateOrderCommand(symbol, "BUY", 100, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Symbol must be one of: PETR4, VALE3, VIIA4")).Should().Be(true);
    }

    [Theory]
    [InlineData("PETR4")]
    [InlineData("VALE3")]
    [InlineData("VIIA4")]
    public async Task Validate_WithValidSymbols_ReturnsNoSymbolError(string symbol)
    {
        var command = new CreateOrderCommand(symbol, "BUY", 100, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.Errors.Any(e => e.ErrorMessage.Contains("Symbol must be one of")).Should().Be(false);
    }

    [Fact]
    public async Task Validate_WithEmptySide_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "", 100, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Side is required")).Should().Be(true);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("HOLD")]
    [InlineData("UNKNOWN")]
    public async Task Validate_WithInvalidSide_ReturnsError(string side)
    {
        var command = new CreateOrderCommand("PETR4", side, 100, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Side must be either BUY or SELL")).Should().Be(true);
    }

    [Theory]
    [InlineData("BUY")]
    [InlineData("SELL")]
    public async Task Validate_WithValidSides_ReturnsNoSideError(string side)
    {
        var command = new CreateOrderCommand("PETR4", side, 100, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.Errors.Any(e => e.ErrorMessage.Contains("Side must be either BUY or SELL")).Should().Be(false);
    }

    [Fact]
    public async Task Validate_WithZeroQuantity_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 0, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Quantity must be greater than 0")).Should().Be(true);
    }

    [Fact]
    public async Task Validate_WithNegativeQuantity_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", -1, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Quantity must be greater than 0")).Should().Be(true);
    }

    [Fact]
    public async Task Validate_WithQuantity100k_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100_000, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Quantity must be less than 100,000")).Should().Be(true);
    }

    [Fact]
    public async Task Validate_WithQuantityGreaterThan100k_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100_001, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Quantity must be less than 100,000")).Should().Be(true);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(99_999)]
    public async Task Validate_WithValidQuantities_ReturnsNoQuantityError(long quantity)
    {
        var command = new CreateOrderCommand("PETR4", "BUY", quantity, 20.50m);

        var result = await _validator.ValidateAsync(command);

        result.Errors.Any(e => e.ErrorMessage.Contains("Quantity must")).Should().Be(false);
    }

    [Fact]
    public async Task Validate_WithZeroPrice_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, 0);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Price must be greater than 0")).Should().Be(true);
    }

    [Fact]
    public async Task Validate_WithNegativePrice_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, -1);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Price must be greater than 0")).Should().Be(true);
    }

    [Fact]
    public async Task Validate_WithPrice1000_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, 1_000);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Price must be less than 1,000")).Should().Be(true);
    }

    [Fact]
    public async Task Validate_WithPriceGreaterThan1000_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, 1_000.01m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Price must be less than 1,000")).Should().Be(true);
    }

    [Fact]
    public async Task Validate_WithPriceNotMultipleOf001_ReturnsError()
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, 20.555m);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Any(e => e.ErrorMessage.Contains("Price must be a multiple of 0.01")).Should().Be(true);
    }

    [Theory]
    [InlineData(0.50)]
    [InlineData(1.50)]
    [InlineData(999.99)]
    [InlineData(100.00)]
    public async Task Validate_WithValidPrices_ReturnsValid(decimal price)
    {
        var command = new CreateOrderCommand("PETR4", "BUY", 100, price);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().Be(true);
    }

    [Fact]
    public async Task Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        var command = new CreateOrderCommand("INVALID", "INVALID", 0, 0);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().Be(false);
        result.Errors.Count.Should().BeGreaterThan(1);
        result.Errors.Any(e => e.ErrorMessage.Contains("Symbol must be one of")).Should().Be(true);
        result.Errors.Any(e => e.ErrorMessage.Contains("Side must be either BUY or SELL")).Should().Be(true);
        result.Errors.Any(e => e.ErrorMessage.Contains("Quantity must be greater than 0")).Should().Be(true);
        result.Errors.Any(e => e.ErrorMessage.Contains("Price must be greater than 0")).Should().Be(true);
    }
}
