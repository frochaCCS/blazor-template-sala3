using ITSupportDesk.Core.Services;

namespace ITSupportDesk.UnitTests;

/// <summary>
/// Mock implementation of IAuthorizationContextService for testing.
/// Allows tests to set the current user and roles explicitly.
/// </summary>
public class MockAuthorizationContextService : IAuthorizationContextService
{
    private string? _currentUserId;
    private HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);

    public void SetCurrentUser(string? userId)
    {
        _currentUserId = userId;
    }

    public void SetRoles(params string[] roles)
    {
        _roles = new(roles, StringComparer.OrdinalIgnoreCase);
    }

    public string? GetCurrentUserId() => _currentUserId;

    public bool HasRole(string role) => _roles.Contains(role);

    public void RequireAuthenticated(string? message = null)
    {
        if (_currentUserId == null)
            throw new UnauthorizedAccessException(message ?? "User is not authenticated");
    }

    public void RequireRole(string role, string? message = null)
    {
        if (!HasRole(role))
            throw new UnauthorizedAccessException(message ?? $"User does not have required role: {role}");
    }

    public void RequireUserId(string userId, string? message = null)
    {
        if (_currentUserId != userId)
            throw new UnauthorizedAccessException(message ?? "User does not have permission to access this resource");
    }
}
