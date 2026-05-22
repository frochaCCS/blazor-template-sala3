using ITSupportDesk.Core.Entities;

namespace ITSupportDesk.UnitTests;

public class TicketCommentTests
{
    [Fact]
    public void Defaults_Are_Set_Correctly()
    {
        var comment = new TicketComment();

        Assert.Equal(0, comment.Id);
        Assert.Equal(0, comment.TicketId);
        Assert.Equal(string.Empty, comment.AuthorId);
        Assert.Equal(string.Empty, comment.Content);
        Assert.Null(comment.Ticket);
        Assert.Null(comment.Author);
    }

    [Fact]
    public void Properties_Can_Be_Set()
    {
        var comment = new TicketComment
        {
            Id = 1,
            TicketId = 42,
            AuthorId = "user-1",
            Content = "This is a test comment"
        };

        Assert.Equal(1, comment.Id);
        Assert.Equal(42, comment.TicketId);
        Assert.Equal("user-1", comment.AuthorId);
        Assert.Equal("This is a test comment", comment.Content);
    }
}
