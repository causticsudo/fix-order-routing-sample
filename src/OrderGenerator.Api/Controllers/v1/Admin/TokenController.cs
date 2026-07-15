using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace OrderGenerator.Api.Controllers.v1.Admin;

[ApiController]
[Route("api/v1/[controller]")]
public class TokenController(IConfiguration config) : ControllerBase
{
    [HttpPost("debug")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GenerateDebugToken([FromQuery] string userId = "test-user")
    {
        try
        {
            //todo: hardcoded para facilitar os testes, isso deveria ser delegado
            // para um Service ou até mesmo API externa, não precisava de auth para esse exemplo
            var secret = config["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret not found in configuration");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "fix-order-generator",
                audience: "fix-order-generator",
                claims:
                [
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userId)
                ],
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new TokenResponse { Token = tokenString });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
