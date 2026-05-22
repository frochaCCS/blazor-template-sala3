using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ITSupportDesk.Core.Entities;

namespace ITSupportDesk.Core.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure roles
        string[] roles = ["Admin", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed admin
        if (await userManager.FindByEmailAsync("admin@template.local") is null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@template.local",
                Email = "admin@template.local",
                DisplayName = "Administrator",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Seed user
        if (await userManager.FindByEmailAsync("user@template.local") is null)
        {
            var user = new ApplicationUser
            {
                UserName = "user@template.local",
                Email = "user@template.local",
                DisplayName = "Demo User",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "User123!");
            await userManager.AddToRoleAsync(user, "User");
        }

        // Seed sample tickets
        var db = serviceProvider.GetRequiredService<AppDbContext>();
        if (!db.SupportTickets.Any())
        {
            var adminUser = await userManager.FindByEmailAsync("admin@template.local");
            var demoUser = await userManager.FindByEmailAsync("user@template.local");

            if (adminUser is not null && demoUser is not null)
            {
                var tickets = new List<SupportTicket>
                {
                    new()
                    {
                        Title = "Laptop not connecting to Wi-Fi",
                        Description = "My laptop cannot find any wireless networks after the latest OS update. I've tried restarting and toggling the Wi-Fi switch.",
                        Category = TicketCategory.Network,
                        Priority = TicketPriority.High,
                        Status = TicketStatus.Open,
                        CreatedById = demoUser.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new()
                    {
                        Title = "Need access to SharePoint site",
                        Description = "I need read/write access to the Marketing team SharePoint site for the upcoming project.",
                        Category = TicketCategory.Access,
                        Priority = TicketPriority.Medium,
                        Status = TicketStatus.InProgress,
                        CreatedById = demoUser.Id,
                        AssignedToId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new()
                    {
                        Title = "Install Visual Studio 2025",
                        Description = "Please install Visual Studio 2025 Enterprise edition on my workstation.",
                        Category = TicketCategory.Software,
                        Priority = TicketPriority.Low,
                        Status = TicketStatus.Resolved,
                        CreatedById = adminUser.Id,
                        AssignedToId = adminUser.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        UpdatedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new()
                    {
                        Title = "Monitor flickering intermittently",
                        Description = "My external monitor flickers every few minutes. It started after I changed desks last week.",
                        Category = TicketCategory.Hardware,
                        Priority = TicketPriority.Medium,
                        Status = TicketStatus.Open,
                        CreatedById = demoUser.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new()
                    {
                        Title = "VPN drops connection frequently",
                        Description = "The corporate VPN disconnects every 15-20 minutes when working from home. Need stable connection for remote work.",
                        Category = TicketCategory.Network,
                        Priority = TicketPriority.Critical,
                        Status = TicketStatus.Open,
                        CreatedById = adminUser.Id,
                        CreatedAt = DateTime.UtcNow.AddHours(-6),
                        UpdatedAt = DateTime.UtcNow.AddHours(-6)
                    }
                };

                db.SupportTickets.AddRange(tickets);
                await db.SaveChangesAsync();

                // Add sample comments to the SharePoint access ticket
                var sharepointTicket = db.SupportTickets.First(t => t.Title.Contains("SharePoint"));
                db.TicketComments.Add(new TicketComment
                {
                    TicketId = sharepointTicket.Id,
                    AuthorId = adminUser.Id,
                    Content = "I've checked your permissions and am processing the access request now.",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
