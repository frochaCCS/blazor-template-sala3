using Microsoft.EntityFrameworkCore;
using ITSupportDesk.Core.Data;
using ITSupportDesk.Core.Entities;

namespace ITSupportDesk.Core.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _db;

    public TicketService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SupportTicket> CreateTicketAsync(string title, string description, TicketCategory category, TicketPriority priority, string createdById)
    {
        var ticket = new SupportTicket
        {
            Title = title,
            Description = description,
            Category = category,
            Priority = priority,
            CreatedById = createdById,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    public async Task<SupportTicket?> GetTicketByIdAsync(int id)
    {
        return await _db.SupportTickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<SupportTicket>> GetTicketsForUserAsync(string userId)
    {
        return await _db.SupportTickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Where(t => t.CreatedById == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SupportTicket>> GetAllTicketsAsync()
    {
        return await _db.SupportTickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateTicketStatusAsync(int ticketId, TicketStatus status)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId);
        if (ticket is null) return false;

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignTicketAsync(int ticketId, string? assignedToId)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId);
        if (ticket is null) return false;

        ticket.AssignedToId = assignedToId;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TicketComment> AddCommentAsync(int ticketId, string authorId, string content)
    {
        // Validate content
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment cannot be empty", nameof(content));

        var trimmedContent = content.Trim();
        if (trimmedContent.Length < 1)
            throw new ArgumentException("Comment must be at least 1 character", nameof(content));

        if (trimmedContent.Length > 2000)
            throw new ArgumentException("Comment must not exceed 2000 characters", nameof(content));

        var comment = new TicketComment
        {
            TicketId = ticketId,
            AuthorId = authorId,
            Content = trimmedContent,
            CreatedAt = DateTime.UtcNow
        };

        _db.TicketComments.Add(comment);
        await _db.SaveChangesAsync();
        return comment;
    }

    public async Task<List<TicketComment>> GetCommentsAsync(int ticketId)
    {
        return await _db.TicketComments
            .Include(c => c.Author)
            .Where(c => c.TicketId == ticketId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
