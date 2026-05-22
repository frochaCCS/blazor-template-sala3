using Microsoft.Playwright;

namespace ITSupportDesk.E2ETests;

public class DashboardFlowTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public DashboardFlowTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Flow1_TicketSummaryCards_ShowOpenInProgressResolvedCounts()
    {
        var page = await _fixture.LoginAsAsync("user@template.local", "User123!");

        // Verify the stats grid is visible
        await Assertions.Expect(page.Locator(".stats-grid")).ToBeVisibleAsync();

        // Verify Open card
        var openCard = page.Locator(".stat-card.stat-open");
        await Assertions.Expect(openCard).ToBeVisibleAsync();
        await Assertions.Expect(openCard.Locator(".stat-label")).ToContainTextAsync("Open");
        await Assertions.Expect(openCard.Locator(".stat-number")).ToBeVisibleAsync();

        // Verify In Progress card
        var inProgressCard = page.Locator(".stat-card.stat-in-progress");
        await Assertions.Expect(inProgressCard).ToBeVisibleAsync();
        await Assertions.Expect(inProgressCard.Locator(".stat-label")).ToContainTextAsync("In Progress");
        await Assertions.Expect(inProgressCard.Locator(".stat-number")).ToBeVisibleAsync();

        // Verify Resolved card
        var resolvedCard = page.Locator(".stat-card.stat-resolved");
        await Assertions.Expect(resolvedCard).ToBeVisibleAsync();
        await Assertions.Expect(resolvedCard.Locator(".stat-label")).ToContainTextAsync("Resolved");
        await Assertions.Expect(resolvedCard.Locator(".stat-number")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Flow2_CreateTicketFromDashboard()
    {
        var page = await _fixture.LoginAsAsync("user@template.local", "User123!");

        // Click the "+ New Ticket" button on the dashboard
        await page.GetByRole(AriaRole.Link, new() { Name = "+ New Ticket" }).ClickAsync();
        await page.WaitForURLAsync("**/tickets/create");

        // Wait for interactive mode
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Fill the form
        await page.Locator("#title").FillAsync("Printer jammed on floor 3");
        await page.Locator("#description").FillAsync("The main printer on floor 3 is jammed and not responding to print jobs.");
        await page.Locator("#category").SelectOptionAsync("Hardware");
        await page.Locator("#priority").SelectOptionAsync("Medium");

        // Submit
        await page.GetByRole(AriaRole.Button, new() { Name = "Create Ticket" }).ClickAsync();

        // Should redirect to ticket detail page
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@".*/tickets/\d+"), new() { Timeout = 10000 });
        await Assertions.Expect(page.Locator("h2")).ToContainTextAsync("Printer jammed on floor 3");
    }

    [Fact]
    public async Task Flow3_AdminSeesTicketsCreatedByOtherUsers()
    {
        var page = await _fixture.LoginAsAsync("admin@template.local", "Admin123!");

        // Navigate to tickets list
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets");

        // Verify at least one ticket row is visible
        var rows = page.Locator(".data-table tbody tr");
        await Assertions.Expect(rows.First).ToBeVisibleAsync();
        var count = await rows.CountAsync();
        Assert.True(count >= 1, $"Expected at least 1 ticket row, found {count}");
    }
}
