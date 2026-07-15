using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Application.Features.Exposures.GetExposures;

namespace OrderGenerator.Api.Controllers.v1.Reports;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ReportController(IMediator mediator, ILogger<ReportController> logger) : ControllerBase
{
    [HttpGet("exposure")]
    [ProducesResponseType(typeof(IReadOnlyList<ExposureResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetExposureReport(
        [FromQuery] string? symbol,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("GetExposureReport request: Symbol={Symbol}", symbol);

            var response = await mediator.Send(new GetExposuresQuery(symbol), cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching exposure report");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
