using Microsoft.EntityFrameworkCore;
using ITSupportDesk.Core.Data;
using ITSupportDesk.Core.Entities;

namespace ITSupportDesk.Core.Services;

/// <summary>
/// Implementation of ITicketService with service-layer authorization checks.
/// Uses IAuthorizationContextService to enforce access control policies.
/// </summary>
public class TicketService : ITicketService
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationContextService _authContext;

    public TicketService(AppDbContext db, IAuthorizationContextService authContext)
    {
        _db = db;
        _authContext = authContext;
    }

    public async Task<SupportTicket> CreateTicketAsync(string title, string description, TicketCategory category, TicketPriority priority, string createdById)
    {
        // Only authenticated users can create tickets
        _authContext.RequireAuthenticated("Only authenticated users can create tickets.");
        _authContext.RequireUserId(createdById, "Users can only create tickets for themselves.");

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
        // Users must be authenticated to view tickets
        _authContext.RequireAuthenticated("Only authenticated users can view tickets.");

        var ticket = await _db.SupportTickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
                .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null)
            return null;

        // Admins can view all tickets; regular users can only view their own
        var userId = _authContext.GetCurrentUserId();
        var isAdmin = _authContext.HasRole("Admin");

        if (!isAdmin && ticket.CreatedById != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view this ticket.");
        }

        return ticket;
    }

    public async Task<List<SupportTicket>> GetTicketsForUserAsync(string userId)
    {
        // Users must be authenticated
        _authContext.RequireAuthenticated("Only authenticated users can view tickets.");
        
        // Users can only view their own tickets; admins can view any user's tickets
        var currentUserId = _authContext.GetCurrentUserId();
        var isAdmin = _authContext.HasRole("Admin");
        
        if (!isAdmin && currentUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to view other users' tickets.");
        }

        return await _db.SupportTickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Where(t => t.CreatedById == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SupportTicket>> GetAllTicketsAsync()
    {
        // Only admins can view all tickets
        _authContext.RequireRole("Admin", "Only admins can view all tickets.");

        return await _db.SupportTickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateTicketStatusAsync(int ticketId, TicketStatus status)
    {
        // Only admins can update ticket status
        _authContext.RequireRole("Admin", "Only admins can update ticket status.");

        var ticket = await _db.SupportTickets.FindAsync(ticketId);
        if (ticket is null) return false;

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignTicketAsync(int ticketId, string? assignedToId)
    {
        // Only admins can assign tickets
        _authContext.RequireRole("Admin", "Only admins can assign tickets.");

        var ticket = await _db.SupportTickets.FindAsync(ticketId);
        if (ticket is null) return false;

        ticket.AssignedToId = assignedToId;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TicketComment> AddCommentAsync(int ticketId, string authorId, string content)
    {
        // Only authenticated users can add comments
        _authContext.RequireAuthenticated("Only authenticated users can add comments.");
        _authContext.RequireUserId(authorId, "Users can only add comments as themselves.");

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
        // Users must be authenticated to view comments
        _authContext.RequireAuthenticated("Only authenticated users can view comments.");

        return await _db.TicketComments
            .Include(c => c.Author)
            .Where(c => c.TicketId == ticketId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
