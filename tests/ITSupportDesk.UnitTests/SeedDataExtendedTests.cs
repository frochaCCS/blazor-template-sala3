using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ITSupportDesk.Core.Data;
using ITSupportDesk.Core.Entities;

namespace ITSupportDesk.UnitTests;

public class SeedDataExtendedTests
{
    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SeedData_Admin_Has_Admin_Role()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@template.local");

        Assert.NotNull(admin);
        Assert.True(await userManager.IsInRoleAsync(admin!, "Admin"));
    }

    [Fact]
    public async Task SeedData_DemoUser_Has_User_Role()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("user@template.local");

        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user!, "User"));
    }

    [Fact]
    public async Task SeedData_Creates_Five_Sample_Tickets()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var tickets = await db.SupportTickets.ToListAsync();
        Assert.Equal(5, tickets.Count);
    }

    [Fact]
    public async Task SeedData_Tickets_Have_Correct_Categories()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var tickets = await db.SupportTickets.ToListAsync();
        var categories = tickets.Select(t => t.Category).Distinct().ToList();

        Assert.Contains(TicketCategory.Network, categories);
        Assert.Contains(TicketCategory.Access, categories);
        Assert.Contains(TicketCategory.Software, categories);
        Assert.Contains(TicketCategory.Hardware, categories);
    }

    [Fact]
    public async Task SeedData_Tickets_Have_Various_Statuses()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var tickets = await db.SupportTickets.ToListAsync();
        var statuses = tickets.Select(t => t.Status).Distinct().ToList();

        Assert.Contains(TicketStatus.Open, statuses);
        Assert.Contains(TicketStatus.InProgress, statuses);
        Assert.Contains(TicketStatus.Resolved, statuses);
    }

    [Fact]
    public async Task SeedData_Creates_Comment_On_SharePoint_Ticket()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var comments = await db.TicketComments.ToListAsync();
        Assert.Single(comments);

        var sharepointTicket = await db.SupportTickets.FirstAsync(t => t.Title.Contains("SharePoint"));
        Assert.Equal(sharepointTicket.Id, comments[0].TicketId);
    }

    [Fact]
    public async Task SeedData_Users_Have_Email_Confirmed()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@template.local");
        var user = await userManager.FindByEmailAsync("user@template.local");

        Assert.True(admin!.EmailConfirmed);
        Assert.True(user!.EmailConfirmed);
    }

    [Fact]
    public async Task SeedData_Ticket_Has_Assignment()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var assignedTickets = await db.SupportTickets
            .Where(t => t.AssignedToId != null)
            .ToListAsync();

        Assert.True(assignedTickets.Count >= 2);
    }

    [Fact]
    public async Task SeedData_Has_Critical_Priority_Ticket()
    {
        using var sp = CreateServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(scope.ServiceProvider);

        var criticalTickets = await db.SupportTickets
            .Where(t => t.Priority == TicketPriority.Critical)
            .ToListAsync();

        Assert.Single(criticalTickets);
        Assert.Contains("VPN", criticalTickets[0].Title);
    }
}
