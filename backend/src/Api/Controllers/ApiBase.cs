using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace RecordLabel.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class ApiBase : ControllerBase
{
    protected Guid LoggedUserId
    {
        get
        {
            var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? throw new InvalidOperationException("The token has no 'sub' claim.");
            return Guid.Parse(subject);
        }
    }
}
