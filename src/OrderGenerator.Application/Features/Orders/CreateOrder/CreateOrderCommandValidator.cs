using FluentValidation;

namespace OrderGenerator.Application.Features.Orders.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Symbol is required")
            .Must(s => new[] { "PETR4", "VALE3", "VIIA4" }.Contains(s.ToUpperInvariant()))
            .WithMessage("Symbol must be one of: PETR4, VALE3, VIIA4");

        RuleFor(x => x.Side)
            .NotEmpty().WithMessage("Side is required")
            .Must(s => new[] { "BUY", "SELL" }.Contains(s.ToUpperInvariant()))
            .WithMessage("Side must be either BUY or SELL");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThan(100_000).WithMessage("Quantity must be less than 100,000");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThan(1_000).WithMessage("Price must be less than 1,000")
            .Must(p => p % 0.01m == 0).WithMessage("Price must be a multiple of 0.01");
    }
}
