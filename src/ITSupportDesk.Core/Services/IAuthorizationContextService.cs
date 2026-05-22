namespace ITSupportDesk.Core.Services;

/// <summary>
/// Service for checking user authorization in the service layer.
/// This enables policy-based authorization checks that are independent of UI/presentation layer.
/// </summary>
public interface IAuthorizationContextService
{
    /// <summary>Gets the current authenticated user ID, or null if not authenticated.</summary>
    string? GetCurrentUserId();

    /// <summary>Checks if the current user has the specified role.</summary>
    bool HasRole(string role);

    /// <summary>Throws UnauthorizedAccessException if the user is not authenticated.</summary>
    void RequireAuthenticated(string? message = null);

    /// <summary>Throws UnauthorizedAccessException if the user does not have the specified role.</summary>
    void RequireRole(string role, string? message = null);

    /// <summary>Throws UnauthorizedAccessException if the user ID does not match the current user.</summary>
    void RequireUserId(string userId, string? message = null);
}
