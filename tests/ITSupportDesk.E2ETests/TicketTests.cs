using Microsoft.Playwright;

namespace ITSupportDesk.E2ETests;

public class TicketTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public TicketTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Ticket_List_Page_Renders_For_User()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");

        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("My Tickets");
        await Assertions.Expect(page.Locator(".data-table")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Ticket_List_Shows_Seeded_Tickets_For_Admin()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");

        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("My Tickets");
        // Admin should see all seeded tickets
        var rows = page.Locator(".data-table tbody tr");
        var count = await rows.CountAsync();
        Assert.True(count >= 5, $"Expected at least 5 seeded tickets, found {count}");
    }

    [Fact]
    public async Task Create_Ticket_Page_Renders_Form()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");

        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Create Support Ticket");
        await Assertions.Expect(page.Locator("#title")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#description")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#category")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#priority")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Create_Ticket_And_View_Detail()
    {
        await using var context = await _fixture.NewUserContextAsync();
        context.SetDefaultTimeout(10000);
        context.SetDefaultNavigationTimeout(10000);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");

        // Wait for interactive mode to be ready
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill out the form
        await page.Locator("#title").FillAsync("Keyboard not working");
        await page.Locator("#description").FillAsync("My keyboard stopped responding after the latest update. Need replacement.");
        await page.Locator("#category").SelectOptionAsync("Hardware");
        await page.Locator("#priority").SelectOptionAsync("High");

        // Submit
        await page.GetByRole(AriaRole.Button, new() { Name = "Create Ticket" }).ClickAsync();

        // Should redirect to ticket detail
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"), new() { Timeout = 10000 });
        await Assertions.Expect(page.Locator("h2")).ToContainTextAsync("Keyboard not working");
    }

    [Fact]
    public async Task Unauthenticated_Tickets_Redirects_To_Login()
    {
        await using var context = await _fixture.NewAnonymousContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");
        await page.WaitForURLAsync("**/Account/Login**");

        Assert.Contains("/Account/Login", page.Url);
    }

    [Fact]
    public async Task Unauthenticated_Create_Ticket_Redirects_To_Login()
    {
        await using var context = await _fixture.NewAnonymousContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");
        await page.WaitForURLAsync("**/Account/Login**");

        Assert.Contains("/Account/Login", page.Url);
    }

    [Fact]
    public async Task Dashboard_Shows_Ticket_Stats()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");

        // Should show stats cards
        await Assertions.Expect(page.Locator(".stats-grid")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".stat-card")).ToHaveCountAsync(4);
    }

    [Fact]
    public async Task Dashboard_Has_New_Ticket_Button()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");

        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "+ New Ticket" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Sidebar_Shows_Ticket_Links()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");

        await Assertions.Expect(page.Locator(".sidebar-nav").GetByRole(AriaRole.Link, new() { Name = "My Tickets" })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".sidebar-nav").GetByRole(AriaRole.Link, new() { Name = "New Ticket" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Ticket_Detail_Shows_Seeded_Ticket()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        var page = await context.NewPageAsync();

        // Navigate to tickets list first
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");
        // Click the first ticket link
        await page.Locator(".data-table tbody tr a").First.ClickAsync();

        // Should be on ticket detail page
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Ticket #");
    }

    [Fact]
    public async Task Admin_Ticket_Detail_Shows_Admin_Actions()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        context.SetDefaultTimeout(5000);
        context.SetDefaultNavigationTimeout(5000);
        var page = await context.NewPageAsync();

        // Navigate to first ticket
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");
        await page.Locator(".data-table tbody tr a").First.ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"));

        // Admin should see admin actions
        await Assertions.Expect(page.GetByText("Admin Actions")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#statusSelect")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#assignSelect")).ToBeVisibleAsync();
    }
}
