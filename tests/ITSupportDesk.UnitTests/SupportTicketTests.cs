using ITSupportDesk.Core.Entities;

namespace ITSupportDesk.UnitTests;

public class SupportTicketTests
{
    [Fact]
    public void Defaults_Are_Set_Correctly()
    {
        var ticket = new SupportTicket();

        Assert.Equal(string.Empty, ticket.Title);
        Assert.Equal(string.Empty, ticket.Description);
        Assert.Equal(TicketPriority.Medium, ticket.Priority);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(TicketCategory.Other, ticket.Category);
        Assert.Equal(string.Empty, ticket.CreatedById);
        Assert.Null(ticket.AssignedToId);
        Assert.Null(ticket.CreatedBy);
        Assert.Null(ticket.AssignedTo);
        Assert.NotNull(ticket.Comments);
        Assert.Empty(ticket.Comments);
    }

    [Fact]
    public void Properties_Can_Be_Set()
    {
        var ticket = new SupportTicket
        {
            Id = 1,
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = TicketPriority.High,
            Status = TicketStatus.InProgress,
            Category = TicketCategory.Hardware,
            CreatedById = "user-1",
            AssignedToId = "admin-1"
        };

        Assert.Equal(1, ticket.Id);
        Assert.Equal("Test Ticket", ticket.Title);
        Assert.Equal("Test Description", ticket.Description);
        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(TicketCategory.Hardware, ticket.Category);
        Assert.Equal("user-1", ticket.CreatedById);
        Assert.Equal("admin-1", ticket.AssignedToId);
    }

    [Fact]
    public void Navigation_Properties_Can_Be_Set()
    {
        var user = new ApplicationUser { DisplayName = "Test User" };
        var admin = new ApplicationUser { DisplayName = "Admin" };

        var ticket = new SupportTicket
        {
            CreatedBy = user,
            AssignedTo = admin
        };

        Assert.Equal("Test User", ticket.CreatedBy?.DisplayName);
        Assert.Equal("Admin", ticket.AssignedTo?.DisplayName);
    }
}
