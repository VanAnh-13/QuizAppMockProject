namespace Quizapp.Infrastructure.Authentication;

using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Quizapp.Application.Abstractions.Authentication;

public class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var subClaim = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
                           ?? httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(subClaim, out var userId))
                return userId;

            return null;
        }
    }
}
