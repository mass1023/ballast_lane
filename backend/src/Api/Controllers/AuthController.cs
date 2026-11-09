using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecordLabel.Application.Users;

namespace RecordLabel.Api.Controllers;

public class AuthController : ApiBase
{
    private readonly IUserService _users;

    public AuthController(IUserService userService)
    {
        _users = userService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = await _users.Register(request, cancellationToken);
        return CreatedAtAction(nameof(Me), user);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthenticateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AuthenticateRequest request, CancellationToken cancellationToken)
    {
        var response = await _users.Authenticate(request, cancellationToken);
        if (response is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid username or password.");

        return Ok(response);
    }

    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        return Ok(await _users.GetById(LoggedUserId, cancellationToken));
    }
}
