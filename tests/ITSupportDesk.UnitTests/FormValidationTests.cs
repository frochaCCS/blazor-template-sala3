using System.ComponentModel.DataAnnotations;

namespace ITSupportDesk.UnitTests;

public class FormValidationTests
{
    private sealed class TicketFormModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be 3–200 characters")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Description is required")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Description must be 10–4000 characters")]
        public string Description { get; set; } = "";
    }

    private List<ValidationResult> ValidateModel(TicketFormModel model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void TicketForm_Valid_With_Correct_Data()
    {
        var model = new TicketFormModel
        {
            Title = "Valid Title",
            Description = "This is a valid description with sufficient length"
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Fact]
    public void TicketForm_Invalid_With_Empty_Title()
    {
        var model = new TicketFormModel
        {
            Title = "",
            Description = "This is a valid description with sufficient length"
        };

        var results = ValidateModel(model);

        Assert.Single(results);
        Assert.Contains("Title is required", results[0].ErrorMessage);
    }

    [Fact]
    public void TicketForm_Invalid_With_Empty_Description()
    {
        var model = new TicketFormModel
        {
            Title = "Valid Title",
            Description = ""
        };

        var results = ValidateModel(model);

        Assert.Single(results);
        Assert.Contains("Description is required", results[0].ErrorMessage);
    }

    [Fact]
    public void TicketForm_Invalid_With_Title_Too_Short()
    {
        var model = new TicketFormModel
        {
            Title = "ab", // 2 characters, minimum is 3
            Description = "This is a valid description with sufficient length"
        };

        var results = ValidateModel(model);

        Assert.Single(results);
        Assert.Contains("Title must be 3–200 characters", results[0].ErrorMessage);
    }

    [Fact]
    public void TicketForm_Invalid_With_Title_Too_Long()
    {
        var model = new TicketFormModel
        {
            Title = new string('a', 201), // 201 characters, maximum is 200
            Description = "This is a valid description with sufficient length"
        };

        var results = ValidateModel(model);

        Assert.Single(results);
        Assert.Contains("Title must be 3–200 characters", results[0].ErrorMessage);
    }

    [Fact]
    public void TicketForm_Invalid_With_Description_Too_Short()
    {
        var model = new TicketFormModel
        {
            Title = "Valid Title",
            Description = "Too short" // 9 characters, minimum is 10
        };

        var results = ValidateModel(model);

        Assert.Single(results);
        Assert.Contains("Description must be 10–4000 characters", results[0].ErrorMessage);
    }

    [Fact]
    public void TicketForm_Invalid_With_Description_Too_Long()
    {
        var model = new TicketFormModel
        {
            Title = "Valid Title",
            Description = new string('a', 4001) // 4001 characters, maximum is 4000
        };

        var results = ValidateModel(model);

        Assert.Single(results);
        Assert.Contains("Description must be 10–4000 characters", results[0].ErrorMessage);
    }

    [Fact]
    public void TicketForm_Valid_With_Minimum_Title_Length()
    {
        var model = new TicketFormModel
        {
            Title = "abc", // Exactly 3 characters
            Description = "This is a valid description with sufficient length"
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Fact]
    public void TicketForm_Valid_With_Maximum_Title_Length()
    {
        var model = new TicketFormModel
        {
            Title = new string('a', 200), // Exactly 200 characters
            Description = "This is a valid description with sufficient length"
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Fact]
    public void TicketForm_Valid_With_Minimum_Description_Length()
    {
        var model = new TicketFormModel
        {
            Title = "Valid Title",
            Description = new string('a', 10) // Exactly 10 characters
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Fact]
    public void TicketForm_Valid_With_Maximum_Description_Length()
    {
        var model = new TicketFormModel
        {
            Title = "Valid Title",
            Description = new string('a', 4000) // Exactly 4000 characters
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Fact]
    public void TicketForm_Invalid_With_Both_Fields_Empty()
    {
        var model = new TicketFormModel
        {
            Title = "",
            Description = ""
        };

        var results = ValidateModel(model);

        Assert.Equal(2, results.Count);
        Assert.Single(results.Where(r => r.ErrorMessage!.Contains("Title")));
        Assert.Single(results.Where(r => r.ErrorMessage!.Contains("Description")));
    }

    [Fact]
    public void TicketForm_Valid_With_Special_Characters_In_Title()
    {
        var model = new TicketFormModel
        {
            Title = "Issue with @#$% characters & symbols!",
            Description = "This is a valid description with sufficient length"
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Fact]
    public void TicketForm_Valid_With_Whitespace_And_Punctuation()
    {
        var model = new TicketFormModel
        {
            Title = "Valid: Title - With... Punctuation!!!",
            Description = "This is a valid description with sufficient length and punctuation!?"
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }

    [Fact]
    public void TicketForm_Invalid_With_Title_Only_Whitespace()
    {
        var model = new TicketFormModel
        {
            Title = "   ", // 3 spaces, but not meaningful content
            Description = "This is a valid description with sufficient length"
        };

        var results = ValidateModel(model);

        // Whitespace-only title is treated as empty by Required validator
        Assert.Single(results);
        Assert.Contains("Title is required", results[0].ErrorMessage);
    }

    [Fact]
    public void TicketForm_Invalid_With_Description_Only_Whitespace()
    {
        var model = new TicketFormModel
        {
            Title = "Valid Title",
            Description = "          " // 10 spaces
        };

        var results = ValidateModel(model);

        // Whitespace-only description is treated as empty by Required validator
        Assert.Single(results);
        Assert.Contains("Description is required", results[0].ErrorMessage);
    }

    [Fact]
    public void TicketForm_Valid_With_Multiline_Description()
    {
        var model = new TicketFormModel
        {
            Title = "Multi-line Test",
            Description = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5 and more"
        };

        var results = ValidateModel(model);

        Assert.Empty(results);
    }
}
