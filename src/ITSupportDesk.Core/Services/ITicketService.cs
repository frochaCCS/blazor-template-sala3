using ITSupportDesk.Core.Entities;

namespace ITSupportDesk.Core.Services;

public interface ITicketService
{
    Task<SupportTicket> CreateTicketAsync(string title, string description, TicketCategory category, TicketPriority priority, string createdById);
    Task<SupportTicket?> GetTicketByIdAsync(int id);
    Task<List<SupportTicket>> GetTicketsForUserAsync(string userId);
    Task<List<SupportTicket>> GetAllTicketsAsync();
    Task<bool> UpdateTicketStatusAsync(int ticketId, TicketStatus status);
    Task<bool> AssignTicketAsync(int ticketId, string? assignedToId);
    Task<TicketComment> AddCommentAsync(int ticketId, string authorId, string content);
    Task<List<TicketComment>> GetCommentsAsync(int ticketId);
}
