namespace OrderGenerator.Api.Controllers.v1.Admin;

public record TokenResponse
{
    public string Token { get; set; } = string.Empty;
}
