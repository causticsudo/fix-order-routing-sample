using MediatR;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;
using OrderGenerator.Domain.ValueObjects;

namespace OrderGenerator.Application.Features.Orders.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _repository;

    public CreateOrderCommandHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var validation = new CreateOrderCommandValidator().Validate(request);
        if (!validation.IsValid)
            throw new ValidationException(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));

        var order = BuildOrder(request);

        await _repository.AddAsync(order, cancellationToken);

        return new CreateOrderResponse(
            order.Id,
            order.Symbol.Value,
            order.Side.Value,
            order.Quantity.Value,
            order.Price.Value,
            order.Status.ToString(),
            order.CreatedAt);
    }

    //todo: não vejo extensão agr, mas poderia ser uma abstract factory ou builder
    // depende mt não sei quantos tipos de order posso ter no futuro
    private static Order BuildOrder(CreateOrderCommand request)
    {
        var symbol = Symbol.Create(request.Symbol);
        var side = OrderSide.Create(request.Side);
        var quantity = Quantity.Create(request.Quantity);
        var price = Price.Create(request.Price);

        var result = Order.Create(symbol, side, quantity, price);
        return (result as ResultSuccess<Order>)!.Value;
    }
}
