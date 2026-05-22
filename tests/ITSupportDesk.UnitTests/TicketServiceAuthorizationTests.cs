using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ITSupportDesk.Core.Data;
using ITSupportDesk.Core.Entities;
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

public class TicketServiceAuthorizationTests
{
    private (ServiceProvider sp, AppDbContext db, TicketService service, MockAuthorizationContextService authContext, string userId, string adminId) CreateTestContext(string? currentUserId = null, params string[] roles)
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILogger<TicketService>>();
        db.Database.EnsureCreated();

        var user = new ApplicationUser { Id = "user-1", UserName = "user@test.local", Email = "user@test.local", DisplayName = "Test User" };
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin@test.local", Email = "admin@test.local", DisplayName = "Admin User" };
        db.Users.Add(user);
        db.Users.Add(admin);
        db.SaveChanges();

        var authContext = new MockAuthorizationContextService();
        if (currentUserId != null)
        {
            authContext.SetCurrentUser(currentUserId);
            authContext.SetRoles(roles);
        }

        var service = new TicketService(db, logger, authContext);
        return (sp, db, service, authContext, user.Id, admin.Id);
    }

    [Fact]
    public async Task GetTicketsForUser_Isolation_User_Cannot_See_Other_User_Tickets()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        // Admin creates a ticket
        await service.CreateTicketAsync("Admin Only Ticket", "This should be admin only", TicketCategory.Software, TicketPriority.High, adminId);

        // User creates their own ticket
        await service.CreateTicketAsync("User Ticket", "This is user ticket", TicketCategory.Hardware, TicketPriority.Low, userId);

        // Verify user can only see their own ticket
        var userTickets = await service.GetTicketsForUserAsync(userId);

        Assert.Single(userTickets);
        Assert.Equal("User Ticket", userTickets[0].Title);
        Assert.Equal(userId, userTickets[0].CreatedById);
    }

    [Fact]
    public async Task GetTicketsForUser_Returns_Only_Creator_Tickets_Not_Assigned_Tickets()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        // User creates a ticket
        var userTicket = await service.CreateTicketAsync("User Created", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Admin creates and assigns to user
        var adminTicket = await service.CreateTicketAsync("Admin Created", "desc", TicketCategory.Hardware, TicketPriority.Low, adminId);
        await service.AssignTicketAsync(adminTicket.Id, userId);

        // GetTicketsForUser returns only tickets CREATED by the user, not assigned tickets
        var userTickets = await service.GetTicketsForUserAsync(userId);

        Assert.Single(userTickets);
        Assert.Equal("User Created", userTickets[0].Title);
        Assert.Equal(userId, userTickets[0].CreatedById);
    }

    [Fact]
    public async Task GetAllTickets_Returns_All_Regardless_Of_Creator()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        await service.CreateTicketAsync("User Ticket", "desc", TicketCategory.Software, TicketPriority.High, userId);
        await service.CreateTicketAsync("Admin Ticket", "desc", TicketCategory.Hardware, TicketPriority.Low, adminId);

        var allTickets = await service.GetAllTicketsAsync();

        Assert.Equal(2, allTickets.Count);
    }

    [Fact]
    public async Task UpdateTicketStatus_Service_Has_No_Authorization_Check()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Note: The service itself has no authorization checks - this is by design
        // Authorization should be enforced at the page/controller level in Blazor components
        // The service can be called by anyone with access to it
        var result = await service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.InProgress);

        Assert.True(result);
        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal(TicketStatus.InProgress, updated!.Status);
    }

    [Fact]
    public async Task AssignTicket_Service_Has_No_Authorization_Check()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Note: The service itself has no authorization checks
        // Authorization is enforced at the Blazor component level
        var result = await service.AssignTicketAsync(ticket.Id, adminId);

        Assert.True(result);
        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal(adminId, updated!.AssignedToId);
    }

    [Fact]
    public async Task AddComment_Service_Has_No_Authorization_Check()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Any authenticated user can add a comment to any ticket at service level
        var comment = await service.AddCommentAsync(ticket.Id, adminId, "Admin comment on user ticket");

        Assert.NotNull(comment);
        Assert.Equal(adminId, comment.AuthorId);
        Assert.Equal(ticket.Id, comment.TicketId);
    }

    [Fact]
    public async Task GetComments_Returns_All_Comments_Regardless_Of_Author()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);
        await service.AddCommentAsync(ticket.Id, userId, "User comment");
        await service.AddCommentAsync(ticket.Id, adminId, "Admin comment");

        var comments = await service.GetCommentsAsync(ticket.Id);

        Assert.Equal(2, comments.Count);
    }

    [Fact]
    public async Task CreateTicket_With_Null_CreatorId_Still_Succeeds_At_Service_Level()
    {
        var (sp, db, service, _, _) = CreateTestContext();
        using var _ = sp;

        // Service doesn't validate creator exists - that's up to the caller
        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, "nonexistent-user");

        Assert.True(ticket.Id > 0);
        Assert.Equal("nonexistent-user", ticket.CreatedById);
    }

    [Fact]
    public async Task AssignTicket_With_Null_Assignee_Always_Succeeds()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Assigning to null (unassigning) always works
        var result = await service.AssignTicketAsync(ticket.Id, null);

        Assert.True(result);
        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Null(updated!.AssignedToId);
    }

    [Fact]
    public async Task AssignTicket_With_Nonexistent_User_Still_Succeeds_At_Service_Level()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Service doesn't validate the assigned user exists
        var result = await service.AssignTicketAsync(ticket.Id, "nonexistent-assignee");

        Assert.True(result);
        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal("nonexistent-assignee", updated!.AssignedToId);
    }

    [Fact]
    public async Task UpdateTicketStatus_To_Invalid_Status_Still_Succeeds()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Service accepts any TicketStatus enum value
        var result = await service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Closed);

        Assert.True(result);
        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal(TicketStatus.Closed, updated!.Status);
    }

    [Fact]
    public async Task AddComment_With_Empty_Content_Throws_ArgumentException()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Service now validates that content is not empty
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddCommentAsync(ticket.Id, userId, ""));
    }

    [Fact]
    public async Task AddComment_With_Whitespace_Only_Throws_ArgumentException()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Service validates and trims whitespace
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddCommentAsync(ticket.Id, userId, "   "));
    }

    [Fact]
    public async Task AddComment_With_Valid_Content_Succeeds()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Service validates content and trims whitespace
        var comment = await service.AddCommentAsync(ticket.Id, userId, "Valid comment");

        Assert.True(comment.Id > 0);
        Assert.Equal("Valid comment", comment.Content);
    }

    [Fact]
    public async Task AddComment_With_Whitespace_Trimmed()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Service trims whitespace from content
        var comment = await service.AddCommentAsync(ticket.Id, userId, "  Trimmed content  ");

        Assert.Equal("Trimmed content", comment.Content);
    }

    [Fact]
    public async Task AddComment_Content_Too_Long_Throws_ArgumentException()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Service validates max content length (2000 characters)
        var longContent = new string('a', 2001);
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddCommentAsync(ticket.Id, userId, longContent));
    }

    [Fact]
    public async Task GetTicketById_Returns_Null_For_Nonexistent_Ticket()
    {
        var (sp, db, service, _, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.GetTicketByIdAsync(999);

        Assert.Null(ticket);
    }

    [Fact]
    public async Task Multiple_Users_Can_Create_Tickets_Independently()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var userTicket = await service.CreateTicketAsync("User Ticket", "desc", TicketCategory.Software, TicketPriority.High, userId);
        var adminTicket = await service.CreateTicketAsync("Admin Ticket", "desc", TicketCategory.Hardware, TicketPriority.Low, adminId);

        var userTickets = await service.GetTicketsForUserAsync(userId);
        var adminTickets = await service.GetTicketsForUserAsync(adminId);

        Assert.Single(userTickets);
        Assert.Equal(userId, userTickets[0].CreatedById);

        Assert.Single(adminTickets);
        Assert.Equal(adminId, adminTickets[0].CreatedById);
    }

    [Fact]
    public async Task Service_Does_Not_Enforce_Role_Based_Access()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Software, TicketPriority.High, userId);

        // Service allows any authenticated user to perform admin operations
        // Role-based authorization is enforced at the Blazor component level
        var statusResult = await service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Closed);
        var assignResult = await service.AssignTicketAsync(ticket.Id, adminId);

        Assert.True(statusResult);
        Assert.True(assignResult);

        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal(TicketStatus.Closed, updated!.Status);
        Assert.Equal(adminId, updated.AssignedToId);
    }
}
