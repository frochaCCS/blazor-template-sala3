using System.ComponentModel.DataAnnotations;

namespace ITSupportDesk.Core.Entities;

public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }

    [Required(ErrorMessage = "Comment cannot be empty")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 2000 characters")]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
