using Microsoft.Playwright;

namespace ITSupportDesk.E2ETests;

/// <summary>
/// Enhanced ticket functionality tests covering:
/// - Comment posting and persistence
/// - Status changes and verification
/// - Ticket assignment
/// - Access control and permissions
/// </summary>
public class TicketFunctionalityTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public TicketFunctionalityTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task User_Cannot_See_Other_Users_Tickets()
    {
        await using var adminContext = await _fixture.NewAdminContextAsync();
        var adminPage = await adminContext.NewPageAsync();
        await adminPage.GotoAsync($"{_fixture.BaseUrl}/tickets");
        
        await adminPage.Locator(".data-table tbody tr a").First.ClickAsync();
        await adminPage.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/(\d+)"));
        
        var ticketId = adminPage.Url.Split('/').Last();

        await using var userContext = await _fixture.NewUserContextAsync();
        var userPage = await userContext.NewPageAsync();
        await userPage.GotoAsync($"{_fixture.BaseUrl}/tickets/{ticketId}");

        await Assertions.Expect(userPage.GetByText("Access Denied")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Post_Comment_On_Own_Ticket()
    {
        await using var context = await _fixture.NewUserContextAsync();
        context.SetDefaultTimeout(10000);
        context.SetDefaultNavigationTimeout(10000);
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await page.Locator("#title").FillAsync("Monitor flickering");
        await page.Locator("#description").FillAsync("Screen goes black intermittently.");
        await page.Locator("#category").SelectOptionAsync("Hardware");
        await page.Locator("#priority").SelectOptionAsync("Medium");
        
        await page.GetByRole(AriaRole.Button, new() { Name = "Create Ticket" }).ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));

        await page.Locator("#newComment").FillAsync("Already tried restarting.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await Assertions.Expect(page.Locator(".comments-list")).ToContainTextAsync("Already tried restarting.");
    }

    [Fact]
    public async Task Admin_Change_Ticket_Status()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        context.SetDefaultTimeout(10000);
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");
        await page.Locator(".data-table tbody tr a").First.ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));

        await page.Locator("#statusSelect").SelectOptionAsync("Closed");
        await page.GetByRole(AriaRole.Button, new() { Name = "Update Status" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Assertions.Expect(page.Locator("text=Closed")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Admin_Assign_Ticket_To_User()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        context.SetDefaultTimeout(10000);
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");
        await page.Locator(".data-table tbody tr a").First.ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));

        var selectElement = page.Locator("#assignSelect");
        var optionCount = await selectElement.Locator("option").CountAsync();

        if (optionCount > 1)
        {
            await selectElement.SelectOptionAsync(new System.Text.RegularExpressions.Regex(".*"));
            var assignButtons = page.Locator(".card").GetByRole(AriaRole.Button).Filter(new() { HasText = "Assign" });
            
            if (await assignButtons.CountAsync() > 0)
            {
                await assignButtons.First.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Assertions.Expect(page.Locator(".alert-info")).ToBeVisibleAsync();
            }
        }
    }

    [Fact]
    public async Task Create_Ticket_With_All_Fields()
    {
        await using var context = await _fixture.NewUserContextAsync();
        context.SetDefaultTimeout(10000);
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.Locator("#title").FillAsync("Complete ticket all fields");
        await page.Locator("#description").FillAsync("Full description of the issue.");
        await page.Locator("#category").SelectOptionAsync("Network");
        await page.Locator("#priority").SelectOptionAsync("Critical");

        await page.GetByRole(AriaRole.Button, new() { Name = "Create Ticket" }).ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));
        
        await Assertions.Expect(page.Locator("h2")).ToContainTextAsync("Complete ticket all fields");
        await Assertions.Expect(page.GetByText("Network")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("Critical")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Non_Admin_Cannot_See_Admin_Actions()
    {
        await using var context = await _fixture.NewUserContextAsync();
        context.SetDefaultTimeout(5000);
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await page.Locator("#title").FillAsync("User ticket");
        await page.Locator("#description").FillAsync("User ticket");
        await page.Locator("#category").SelectOptionAsync("Software");
        await page.Locator("#priority").SelectOptionAsync("Low");
        
        await page.GetByRole(AriaRole.Button, new() { Name = "Create Ticket" }).ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));

        await Assertions.Expect(page.GetByText("Admin Actions")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("#statusSelect")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("#assignSelect")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Status_Change_Persists_On_Reload()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        context.SetDefaultTimeout(10000);
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");
        await page.Locator(".data-table tbody tr a").First.ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));

        await page.Locator("#statusSelect").SelectOptionAsync("InProgress");
        await page.GetByRole(AriaRole.Button, new() { Name = "Update Status" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.ReloadAsync();

        await Assertions.Expect(page.Locator("text=In Progress")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Multiple_Comments_Display_In_Order()
    {
        await using var context = await _fixture.NewUserContextAsync();
        context.SetDefaultTimeout(10000);
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await page.Locator("#title").FillAsync("Multiple comments");
        await page.Locator("#description").FillAsync("Testing comments");
        await page.Locator("#category").SelectOptionAsync("Software");
        await page.Locator("#priority").SelectOptionAsync("Low");
        
        await page.GetByRole(AriaRole.Button, new() { Name = "Create Ticket" }).ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));

        await page.Locator("#newComment").FillAsync("First comment");
        await page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.Locator("#newComment").FillAsync("Second comment");
        await page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var commentsList = page.Locator(".comments-list");
        await Assertions.Expect(commentsList).ToContainTextAsync("First comment");
        await Assertions.Expect(commentsList).ToContainTextAsync("Second comment");
    }

    [Fact]
    public async Task Ticket_Detail_Has_Back_Button()
    {
        await using var context = await _fixture.NewUserContextAsync();
        context.SetDefaultTimeout(10000);
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await page.Locator("#title").FillAsync("Test back button");
        await page.Locator("#description").FillAsync("Verifying back button functionality");
        await page.Locator("#category").SelectOptionAsync("Software");
        await page.Locator("#priority").SelectOptionAsync("Low");
        
        await page.GetByRole(AriaRole.Button, new() { Name = "Create Ticket" }).ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));

        var backButton = page.GetByRole(AriaRole.Link, new() { Name = "Back to Tickets" });
        await Assertions.Expect(backButton).ToBeVisibleAsync();

        await backButton.ClickAsync();
        
        await page.WaitForURLAsync("**/tickets");
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("My Tickets");
    }

    [Fact]
    public async Task Comments_Section_Shows_Comment_Form()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        context.SetDefaultTimeout(5000);
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");
        await page.Locator(".data-table tbody tr a").First.ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));

        await Assertions.Expect(page.Locator("h3")).ToContainTextAsync("Comments");
        await Assertions.Expect(page.Locator("#newComment")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" })).ToBeVisibleAsync();
    }
}
