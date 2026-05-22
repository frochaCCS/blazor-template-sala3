using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ITSupportDesk.Core.Data;
using ITSupportDesk.Core.Entities;

namespace ITSupportDesk.UnitTests;

public class AppDbContextTests : IAsyncLifetime
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

    private (ServiceProvider sp, AppDbContext db) CreateTestContext()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        var services = new ServiceCollection();
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={_dbPath}"));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        services.AddLogging();

        _sp = services.BuildServiceProvider();
        var db = _sp.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return (_sp, db);
    }

    private ApplicationUser SeedUser(AppDbContext db, string id, string displayName)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@test.local",
            Email = $"{id}@test.local",
            DisplayName = displayName
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public void DbContext_Has_SupportTickets_DbSet()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        Assert.NotNull(db.SupportTickets);
    }

    [Fact]
    public void DbContext_Has_TicketComments_DbSet()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        Assert.NotNull(db.TicketComments);
    }

    [Fact]
    public async Task Can_Save_And_Retrieve_Ticket()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        var user = SeedUser(db, "user-1", "Test User");
        var ticket = new SupportTicket
        {
            Title = "Test",
            Description = "Test description",
            CreatedById = user.Id,
            Category = TicketCategory.Software,
            Priority = TicketPriority.High
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();

        var retrieved = await db.SupportTickets.FindAsync(ticket.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Test", retrieved!.Title);
    }

    [Fact]
    public async Task Can_Save_And_Retrieve_Comment()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        var user = SeedUser(db, "user-1", "Test User");
        var ticket = new SupportTicket
        {
            Title = "Test",
            Description = "desc",
            CreatedById = user.Id
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();

        var comment = new TicketComment
        {
            TicketId = ticket.Id,
            AuthorId = user.Id,
            Content = "Test comment"
        };
        db.TicketComments.Add(comment);
        await db.SaveChangesAsync();

        var retrieved = await db.TicketComments.FindAsync(comment.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Test comment", retrieved!.Content);
    }

    [Fact]
    public async Task Ticket_CreatedBy_Relationship_Works()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        var user = SeedUser(db, "user-1", "Test User");
        var ticket = new SupportTicket
        {
            Title = "Test",
            Description = "desc",
            CreatedById = user.Id
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();

        var retrieved = await db.SupportTickets
            .Include(t => t.CreatedBy)
            .FirstAsync(t => t.Id == ticket.Id);

        Assert.NotNull(retrieved.CreatedBy);
        Assert.Equal("Test User", retrieved.CreatedBy!.DisplayName);
    }

    [Fact]
    public async Task Ticket_AssignedTo_Relationship_Works()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        var user = SeedUser(db, "user-1", "Test User");
        var admin = SeedUser(db, "admin-1", "Admin User");
        var ticket = new SupportTicket
        {
            Title = "Test",
            Description = "desc",
            CreatedById = user.Id,
            AssignedToId = admin.Id
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();

        var retrieved = await db.SupportTickets
            .Include(t => t.AssignedTo)
            .FirstAsync(t => t.Id == ticket.Id);

        Assert.NotNull(retrieved.AssignedTo);
        Assert.Equal("Admin User", retrieved.AssignedTo!.DisplayName);
    }

    [Fact]
    public async Task Comment_Ticket_Relationship_Works()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        var user = SeedUser(db, "user-1", "Test User");
        var ticket = new SupportTicket
        {
            Title = "Test",
            Description = "desc",
            CreatedById = user.Id
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();

        var comment = new TicketComment
        {
            TicketId = ticket.Id,
            AuthorId = user.Id,
            Content = "Test comment"
        };
        db.TicketComments.Add(comment);
        await db.SaveChangesAsync();

        var retrieved = await db.TicketComments
            .Include(c => c.Ticket)
            .FirstAsync(c => c.Id == comment.Id);

        Assert.NotNull(retrieved.Ticket);
        Assert.Equal("Test", retrieved.Ticket!.Title);
    }

    [Fact]
    public async Task Comment_Author_Relationship_Works()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        var user = SeedUser(db, "user-1", "Test User");
        var ticket = new SupportTicket
        {
            Title = "Test",
            Description = "desc",
            CreatedById = user.Id
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();

        var comment = new TicketComment
        {
            TicketId = ticket.Id,
            AuthorId = user.Id,
            Content = "Test comment"
        };
        db.TicketComments.Add(comment);
        await db.SaveChangesAsync();

        var retrieved = await db.TicketComments
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id);

        Assert.NotNull(retrieved.Author);
        Assert.Equal("Test User", retrieved.Author!.DisplayName);
    }

    [Fact]
    public async Task Ticket_Comments_Collection_Navigation_Works()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        var user = SeedUser(db, "user-1", "Test User");
        var ticket = new SupportTicket
        {
            Title = "Test",
            Description = "desc",
            CreatedById = user.Id
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();

        db.TicketComments.Add(new TicketComment { TicketId = ticket.Id, AuthorId = user.Id, Content = "Comment 1" });
        db.TicketComments.Add(new TicketComment { TicketId = ticket.Id, AuthorId = user.Id, Content = "Comment 2" });
        await db.SaveChangesAsync();

        var retrieved = await db.SupportTickets
            .Include(t => t.Comments)
            .FirstAsync(t => t.Id == ticket.Id);

        Assert.Equal(2, retrieved.Comments.Count);
    }

    [Fact]
    public async Task Ticket_AssignedTo_Can_Be_Null()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        var user = SeedUser(db, "user-1", "Test User");
        var ticket = new SupportTicket
        {
            Title = "Test",
            Description = "desc",
            CreatedById = user.Id,
            AssignedToId = null
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();

        var retrieved = await db.SupportTickets
            .Include(t => t.AssignedTo)
            .FirstAsync(t => t.Id == ticket.Id);

        Assert.Null(retrieved.AssignedToId);
        Assert.Null(retrieved.AssignedTo);
    }

    [Fact]
    public async Task Enum_Values_Are_Stored_Correctly()
    {
        var (sp, db) = CreateTestContext();
        using var _ = sp;

        var user = SeedUser(db, "user-1", "Test User");

        var ticket = new SupportTicket
        {
            Title = "Enum Test",
            Description = "desc",
            CreatedById = user.Id,
            Status = TicketStatus.Resolved,
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Network
        };
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();

        // Detach and reload to ensure values survive round-trip
        db.Entry(ticket).State = EntityState.Detached;
        var retrieved = await db.SupportTickets.FindAsync(ticket.Id);

        Assert.Equal(TicketStatus.Resolved, retrieved!.Status);
        Assert.Equal(TicketPriority.Critical, retrieved.Priority);
        Assert.Equal(TicketCategory.Network, retrieved.Category);
    }
}
