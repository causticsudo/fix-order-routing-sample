using AwesomeAssertions;
using OrderGenerator.Domain.Aggregates;
using OrderGenerator.Domain.Aggregates.Enumerators;
using Xunit;

namespace OrderGenerator.UnitTests.Domain;

public class OrderEventTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesEventWithGeneratedIdAndTimestamp()
    {
        var orderId = Guid.NewGuid();
        var correlationKey = orderId.ToString();

        var orderEvent = OrderEvent.Create(orderId, correlationKey, OrderEventType.Created);

        orderEvent.Id.Should().NotBe(Guid.Empty);
        orderEvent.OrderId.Should().Be(orderId);
        orderEvent.CorrelationKey.Should().Be(correlationKey);
        orderEvent.EventType.Should().Be(OrderEventType.Created);
        orderEvent.Details.Should().BeNull();
        orderEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithRejectedTypeAndDetails_StoresDetails()
    {
        var orderId = Guid.NewGuid();

        var orderEvent = OrderEvent.Create(orderId, orderId.ToString(), OrderEventType.Rejected, "Exposure limit exceeded");

        orderEvent.EventType.Should().Be(OrderEventType.Rejected);
        orderEvent.Details.Should().Be("Exposure limit exceeded");
    }
}
