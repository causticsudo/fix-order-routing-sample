using MediatR;
using OrderGenerator.Application.Abstractions;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Domain.Abstractions;
using OrderGenerator.Domain.Aggregates;
using OrderGenerator.Domain.Aggregates.Enumerators;
using OrderGenerator.Domain.ValueObjects;

namespace OrderGenerator.Application.Features.Orders.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IOrderRepository _repository;
    private readonly IFixOrderInitiator _fixInitiator;
    private readonly IOrderEventRepository _orderEventRepository;

    public CreateOrderCommandHandler(
        IOrderRepository repository,
        IFixOrderInitiator fixInitiator,
        IOrderEventRepository orderEventRepository)
    {
        _repository = repository;
        _fixInitiator = fixInitiator;
        _orderEventRepository = orderEventRepository;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var validation = await new CreateOrderCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors.Select(e => e.ErrorMessage));
        }

        var order = BuildOrder(request);

        await _repository.AddAsync(order, cancellationToken);
        await _orderEventRepository.AddAsync(
            OrderEvent.Create(order.Id, order.Id.ToString(), OrderEventType.Created),
            cancellationToken);

        order.MarkAsSubmitted();
        await _repository.UpdateAsync(order, cancellationToken);
        await _orderEventRepository.AddAsync(
            OrderEvent.Create(order.Id, order.Id.ToString(), OrderEventType.Submitted),
            cancellationToken);

        await _fixInitiator.SendNewOrderSingleAsync(order, cancellationToken);

        return new CreateOrderResponse(
            order.Id,
            order.Symbol.Value,
            order.Side.Value,
            order.Quantity.Value,
            order.Price.Value,
            order.Status.ToString(),
            order.CreatedAt,
            order.RejectionReason);
    }

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
