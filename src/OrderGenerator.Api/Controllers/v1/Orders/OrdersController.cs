using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Application.Common;
using OrderGenerator.Application.Exceptions;
using OrderGenerator.Application.Features.Orders.CreateOrder;
using OrderGenerator.Application.Features.Orders.GetEvents;
using OrderGenerator.Application.Features.Orders.GetOrder;
using OrderGenerator.Application.Features.Orders.ListOrders;

namespace OrderGenerator.Api.Controllers.v1.Orders;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController(IMediator mediator, ILogger<OrdersController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<CreateOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("ListOrders request: Page={Page}, PageSize={PageSize}", page, pageSize);

            var query = new ListOrdersQuery(page, pageSize);
            var response = await mediator.Send(query, cancellationToken);

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation error: {Error}", ex.Message);
            return BadRequest(new { errors = ex.Errors });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing orders");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "CreateOrder request: Symbol={Symbol}, Side={Side}, Quantity={Quantity}, Price={Price}",
                request.Symbol, request.Side, request.Quantity, request.Price);

            var command = new CreateOrderCommand(
                request.Symbol,
                request.Side,
                request.Quantity,
                request.Price);

            var response = await mediator.Send(command, cancellationToken);

            return CreatedAtAction(nameof(GetOrder), new { id = response.OrderId }, response);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation error: {Error}", ex.Message);
            return BadRequest(new { errors = ex.Errors });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrder(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("GetOrder request: OrderId={OrderId}", id);

            var query = new GetOrderQuery(id);
            var response = await mediator.Send(query, cancellationToken);

            return Ok(response);
        }
        catch (OrderNotFoundException ex)
        {
            logger.LogWarning("Order not found: {Error}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching order");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("events")]
    [ProducesResponseType(typeof(PagedResponse<OrderEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? orderId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("GetEvents request: Page={Page}, PageSize={PageSize}, OrderId={OrderId}", page, pageSize, orderId);

            var query = new GetEventsQuery(page, pageSize, orderId);
            var response = await mediator.Send(query, cancellationToken);

            return Ok(response);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation error: {Error}", ex.Message);
            return BadRequest(new { errors = ex.Errors });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching events");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
