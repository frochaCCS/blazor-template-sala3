using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ITSupportDesk.Core.Data;
using ITSupportDesk.Core.Entities;
using ITSupportDesk.Core.Services;

namespace ITSupportDesk.UnitTests;

public class TicketServiceTests : IAsyncLifetime
{
    private string? _dbPath;
    private ServiceProvider? _sp;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_sp != null) await _sp.DisposeAsync();
        if (_dbPath != null && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    private (ServiceProvider sp, AppDbContext db, TicketService service, string userId, string adminId) CreateTestContext()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={_dbPath}"));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // Seed test users directly in the DB
        var user = new ApplicationUser { Id = "user-1", UserName = "user@test.local", Email = "user@test.local", DisplayName = "Test User" };
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin@test.local", Email = "admin@test.local", DisplayName = "Admin User" };
        db.Users.Add(user);
        db.Users.Add(admin);
        db.SaveChanges();

        var logger = sp.GetRequiredService<ILogger<TicketService>>();
        var authContext = new MockAuthorizationContextService();
        authContext.SetCurrentUser(user.Id);
        var service = new TicketService(db, logger, authContext);
        return (sp, db, service, user.Id, admin.Id);
    }

    [Fact]
    public async Task CreateTicket_Succeeds_With_Valid_Data()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "Description here", TicketCategory.Software, TicketPriority.High, userId);

        Assert.True(ticket.Id > 0);
        Assert.Equal("Test", ticket.Title);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(userId, ticket.CreatedById);
    }

    [Fact]
    public async Task GetTicketsForUser_Returns_Only_User_Tickets()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        await service.CreateTicketAsync("User Ticket", "desc", TicketCategory.Hardware, TicketPriority.Low, userId);
        await service.CreateTicketAsync("Admin Ticket", "desc", TicketCategory.Software, TicketPriority.Medium, adminId);

        var userTickets = await service.GetTicketsForUserAsync(userId);
        Assert.Single(userTickets);
        Assert.Equal("User Ticket", userTickets[0].Title);
    }

    [Fact]
    public async Task GetAllTickets_Returns_All()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        await service.CreateTicketAsync("Ticket 1", "desc", TicketCategory.Hardware, TicketPriority.Low, userId);
        await service.CreateTicketAsync("Ticket 2", "desc", TicketCategory.Software, TicketPriority.Medium, adminId);

        var allTickets = await service.GetAllTicketsAsync();
        Assert.Equal(2, allTickets.Count);
    }

    [Fact]
    public async Task UpdateTicketStatus_Changes_Status()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Medium, userId);
        var result = await service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.InProgress);

        Assert.True(result);
        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal(TicketStatus.InProgress, updated!.Status);
    }

    [Fact]
    public async Task UpdateTicketStatus_Returns_False_For_Missing_Ticket()
    {
        var (sp, db, service, _, _) = CreateTestContext();
        using var _ = sp;

        var result = await service.UpdateTicketStatusAsync(999, TicketStatus.Closed);
        Assert.False(result);
    }

    [Fact]
    public async Task AssignTicket_Sets_Assignee()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Medium, userId);
        var result = await service.AssignTicketAsync(ticket.Id, adminId);

        Assert.True(result);
        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal(adminId, updated!.AssignedToId);
    }

    [Fact]
    public async Task AssignTicket_Returns_False_For_Missing_Ticket()
    {
        var (sp, db, service, _, _) = CreateTestContext();
        using var _ = sp;

        var result = await service.AssignTicketAsync(999, "user-1");
        Assert.False(result);
    }

    [Fact]
    public async Task AddComment_Creates_Comment()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Medium, userId);
        var comment = await service.AddCommentAsync(ticket.Id, userId, "This is a comment");

        Assert.True(comment.Id > 0);
        Assert.Equal(ticket.Id, comment.TicketId);
        Assert.Equal("This is a comment", comment.Content);
    }

    [Fact]
    public async Task GetComments_Returns_Comments_For_Ticket()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Medium, userId);
        await service.AddCommentAsync(ticket.Id, userId, "Comment 1");
        await service.AddCommentAsync(ticket.Id, adminId, "Comment 2");

        var comments = await service.GetCommentsAsync(ticket.Id);
        Assert.Equal(2, comments.Count);
    }

    [Fact]
    public async Task GetTicketById_Returns_Null_For_Missing()
    {
        var (sp, db, service, _, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.GetTicketByIdAsync(999);
        Assert.Null(ticket);
    }
}
