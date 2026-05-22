using System.Security.Claims;
using ITSupportDesk.Core.Entities;
using ITSupportDesk.Core.Services;
using Microsoft.AspNetCore.Identity;

namespace ITSupportDesk.Web.Services;

/// <summary>
/// Implementation of authorization context service that reads from HttpContext and Identity.
/// </summary>
public class AuthorizationContextService : IAuthorizationContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthorizationContextService(IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public string? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public bool HasRole(string role)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole(role) ?? false;
    }

    public void RequireAuthenticated(string? message = null)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException(message ?? "User must be authenticated.");
        }
    }

    public void RequireRole(string role, string? message = null)
    {
        if (!HasRole(role))
        {
            throw new UnauthorizedAccessException(message ?? $"User must have '{role}' role.");
        }
    }

    public void RequireUserId(string userId, string? message = null)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId != userId && !HasRole("Admin"))
        {
            throw new UnauthorizedAccessException(message ?? "User is not authorized to access this resource.");
        }
    }
}
