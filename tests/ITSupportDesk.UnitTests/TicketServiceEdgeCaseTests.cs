using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ITSupportDesk.Core.Data;
using ITSupportDesk.Core.Entities;
using ITSupportDesk.Core.Services;

namespace ITSupportDesk.UnitTests;

public class TicketServiceEdgeCaseTests
{
    private (ServiceProvider sp, AppDbContext db, TicketService service, string userId, string adminId) CreateTestContext()
    {
        var services = new ServiceCollection();
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        var user = new ApplicationUser { Id = "user-1", UserName = "user@test.local", Email = "user@test.local", DisplayName = "Test User" };
        var admin = new ApplicationUser { Id = "admin-1", UserName = "admin@test.local", Email = "admin@test.local", DisplayName = "Admin User" };
        db.Users.Add(user);
        db.Users.Add(admin);
        db.SaveChanges();

        var service = new TicketService(db);
        return (sp, db, service, user.Id, admin.Id);
    }

    [Fact]
    public async Task CreateTicket_Sets_All_Properties_Correctly()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var beforeCreate = DateTime.UtcNow;
        var ticket = await service.CreateTicketAsync("My Title", "My detailed description", TicketCategory.Network, TicketPriority.Critical, userId);

        Assert.Equal("My Title", ticket.Title);
        Assert.Equal("My detailed description", ticket.Description);
        Assert.Equal(TicketCategory.Network, ticket.Category);
        Assert.Equal(TicketPriority.Critical, ticket.Priority);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(userId, ticket.CreatedById);
        Assert.Null(ticket.AssignedToId);
        Assert.True(ticket.CreatedAt >= beforeCreate);
        Assert.True(ticket.UpdatedAt >= beforeCreate);
    }

    [Fact]
    public async Task GetTicketById_Returns_Ticket_With_Navigation_Properties()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var created = await service.CreateTicketAsync("Nav Test", "desc", TicketCategory.Software, TicketPriority.High, userId);
        await service.AssignTicketAsync(created.Id, adminId);
        await service.AddCommentAsync(created.Id, userId, "Test comment");

        var ticket = await service.GetTicketByIdAsync(created.Id);

        Assert.NotNull(ticket);
        Assert.NotNull(ticket!.CreatedBy);
        Assert.Equal("Test User", ticket.CreatedBy!.DisplayName);
        Assert.NotNull(ticket.AssignedTo);
        Assert.Equal("Admin User", ticket.AssignedTo!.DisplayName);
        Assert.Single(ticket.Comments);
        Assert.NotNull(ticket.Comments[0].Author);
    }

    [Fact]
    public async Task GetTicketsForUser_Returns_Empty_When_No_Tickets()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var tickets = await service.GetTicketsForUserAsync(userId);

        Assert.NotNull(tickets);
        Assert.Empty(tickets);
    }

    [Fact]
    public async Task GetAllTickets_Returns_Empty_When_No_Tickets()
    {
        var (sp, db, service, _, _) = CreateTestContext();
        using var _ = sp;

        var tickets = await service.GetAllTicketsAsync();

        Assert.NotNull(tickets);
        Assert.Empty(tickets);
    }

    [Fact]
    public async Task GetTicketsForUser_Returns_Tickets_In_Descending_Order()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        await service.CreateTicketAsync("First", "desc", TicketCategory.Other, TicketPriority.Low, userId);
        await Task.Delay(10); // Ensure different timestamps
        await service.CreateTicketAsync("Second", "desc", TicketCategory.Other, TicketPriority.Low, userId);

        var tickets = await service.GetTicketsForUserAsync(userId);

        Assert.Equal(2, tickets.Count);
        Assert.Equal("Second", tickets[0].Title);
        Assert.Equal("First", tickets[1].Title);
    }

    [Fact]
    public async Task GetComments_Returns_Empty_For_Ticket_With_No_Comments()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("No Comments", "desc", TicketCategory.Other, TicketPriority.Low, userId);

        var comments = await service.GetCommentsAsync(ticket.Id);

        Assert.NotNull(comments);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_Returns_Comments_In_Descending_Order()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Low, userId);
        await service.AddCommentAsync(ticket.Id, userId, "First comment");
        await Task.Delay(10);
        await service.AddCommentAsync(ticket.Id, userId, "Second comment");

        var comments = await service.GetCommentsAsync(ticket.Id);

        Assert.Equal(2, comments.Count);
        Assert.Equal("Second comment", comments[0].Content);
        Assert.Equal("First comment", comments[1].Content);
    }

    [Fact]
    public async Task GetComments_Does_Not_Return_Comments_From_Other_Tickets()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket1 = await service.CreateTicketAsync("Ticket 1", "desc", TicketCategory.Other, TicketPriority.Low, userId);
        var ticket2 = await service.CreateTicketAsync("Ticket 2", "desc", TicketCategory.Other, TicketPriority.Low, userId);

        await service.AddCommentAsync(ticket1.Id, userId, "Comment for ticket 1");
        await service.AddCommentAsync(ticket2.Id, userId, "Comment for ticket 2");

        var comments1 = await service.GetCommentsAsync(ticket1.Id);
        var comments2 = await service.GetCommentsAsync(ticket2.Id);

        Assert.Single(comments1);
        Assert.Equal("Comment for ticket 1", comments1[0].Content);
        Assert.Single(comments2);
        Assert.Equal("Comment for ticket 2", comments2[0].Content);
    }

    [Fact]
    public async Task UpdateTicketStatus_Updates_Timestamp()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Medium, userId);
        var originalUpdatedAt = ticket.UpdatedAt;

        await Task.Delay(10);
        await service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Resolved);

        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.True(updated!.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task AssignTicket_Updates_Timestamp()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Medium, userId);
        var originalUpdatedAt = ticket.UpdatedAt;

        await Task.Delay(10);
        await service.AssignTicketAsync(ticket.Id, adminId);

        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.True(updated!.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task AssignTicket_Can_Unassign_With_Null()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Medium, userId);
        await service.AssignTicketAsync(ticket.Id, adminId);

        var result = await service.AssignTicketAsync(ticket.Id, null);

        Assert.True(result);
        var updated = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Null(updated!.AssignedToId);
    }

    [Fact]
    public async Task UpdateTicketStatus_Through_All_States()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Medium, userId);
        Assert.Equal(TicketStatus.Open, ticket.Status);

        await service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.InProgress);
        var t1 = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal(TicketStatus.InProgress, t1!.Status);

        await service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Resolved);
        var t2 = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal(TicketStatus.Resolved, t2!.Status);

        await service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Closed);
        var t3 = await service.GetTicketByIdAsync(ticket.Id);
        Assert.Equal(TicketStatus.Closed, t3!.Status);
    }

    [Fact]
    public async Task CreateTicket_With_Each_Category()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        foreach (var category in Enum.GetValues<TicketCategory>())
        {
            var ticket = await service.CreateTicketAsync($"Cat {category}", "desc", category, TicketPriority.Low, userId);
            Assert.Equal(category, ticket.Category);
        }

        var all = await service.GetAllTicketsAsync();
        Assert.Equal(Enum.GetValues<TicketCategory>().Length, all.Count);
    }

    [Fact]
    public async Task CreateTicket_With_Each_Priority()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        foreach (var priority in Enum.GetValues<TicketPriority>())
        {
            var ticket = await service.CreateTicketAsync($"Pri {priority}", "desc", TicketCategory.Other, priority, userId);
            Assert.Equal(priority, ticket.Priority);
        }

        var all = await service.GetAllTicketsAsync();
        Assert.Equal(Enum.GetValues<TicketPriority>().Length, all.Count);
    }

    [Fact]
    public async Task AddComment_Sets_Correct_Properties()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var beforeAdd = DateTime.UtcNow;
        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Low, userId);
        var comment = await service.AddCommentAsync(ticket.Id, userId, "My comment content");

        Assert.Equal(ticket.Id, comment.TicketId);
        Assert.Equal(userId, comment.AuthorId);
        Assert.Equal("My comment content", comment.Content);
        Assert.True(comment.CreatedAt >= beforeAdd);
    }

    [Fact]
    public async Task GetComments_Includes_Author_Navigation()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Low, userId);
        await service.AddCommentAsync(ticket.Id, userId, "Test comment");

        var comments = await service.GetCommentsAsync(ticket.Id);

        Assert.Single(comments);
        Assert.NotNull(comments[0].Author);
        Assert.Equal("Test User", comments[0].Author!.DisplayName);
    }

    [Fact]
    public async Task GetAllTickets_Includes_CreatedBy_Navigation()
    {
        var (sp, db, service, userId, _) = CreateTestContext();
        using var _ = sp;

        await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Low, userId);

        var tickets = await service.GetAllTicketsAsync();

        Assert.Single(tickets);
        Assert.NotNull(tickets[0].CreatedBy);
        Assert.Equal("Test User", tickets[0].CreatedBy!.DisplayName);
    }

    [Fact]
    public async Task GetTicketsForUser_Includes_Navigation_Properties()
    {
        var (sp, db, service, userId, adminId) = CreateTestContext();
        using var _ = sp;

        var ticket = await service.CreateTicketAsync("Test", "desc", TicketCategory.Other, TicketPriority.Low, userId);
        await service.AssignTicketAsync(ticket.Id, adminId);

        var tickets = await service.GetTicketsForUserAsync(userId);

        Assert.Single(tickets);
        Assert.NotNull(tickets[0].CreatedBy);
        Assert.NotNull(tickets[0].AssignedTo);
    }

    [Fact]
    public async Task GetComments_Returns_Empty_For_Nonexistent_Ticket()
    {
        var (sp, db, service, _, _) = CreateTestContext();
        using var _ = sp;

        var comments = await service.GetCommentsAsync(999);

        Assert.NotNull(comments);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetTicketsForUser_Returns_Empty_For_Nonexistent_User()
    {
        var (sp, db, service, _, _) = CreateTestContext();
        using var _ = sp;

        var tickets = await service.GetTicketsForUserAsync("nonexistent-user");

        Assert.NotNull(tickets);
        Assert.Empty(tickets);
    }
}
