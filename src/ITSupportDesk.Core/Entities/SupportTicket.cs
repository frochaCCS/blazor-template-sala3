namespace ITSupportDesk.Core.Entities;

public class SupportTicket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketCategory Category { get; set; } = TicketCategory.Other;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser? CreatedBy { get; set; }

    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }

    public List<TicketComment> Comments { get; set; } = new();
}
