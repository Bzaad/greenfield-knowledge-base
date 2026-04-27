using System.Security.Claims;

namespace KnowledgeBase.Api.Services;

// Reads identity from the Azure AD JWT bearer token. The 'oid' claim is the stable
// Azure AD object ID, preferred over 'sub' which can change across tenants.
// Falls back to a local dev identity when no auth is configured (Development only).
public class HttpContextCurrentUserService(IHttpContextAccessor accessor, IWebHostEnvironment env) : ICurrentUserService
{
    private ClaimsPrincipal User =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("No active HTTP context.");

    public string UserId =>
        User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? (env.IsDevelopment() ? "dev-user" : throw new InvalidOperationException("User identity claim is missing."));

    public string DisplayName =>
        User.FindFirstValue("name") ?? User.FindFirstValue(ClaimTypes.Name)
        ?? (env.IsDevelopment() ? "Dev User" : UserId);
}
