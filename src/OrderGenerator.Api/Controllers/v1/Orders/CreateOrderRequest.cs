namespace OrderGenerator.Api.Controllers.v1.Orders;

public record CreateOrderRequest(
    string Symbol,
    string Side,
    long Quantity,
    decimal Price);
