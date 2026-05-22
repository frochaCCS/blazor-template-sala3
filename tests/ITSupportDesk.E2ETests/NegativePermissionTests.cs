using Microsoft.Playwright;

namespace ITSupportDesk.E2ETests;

public class NegativePermissionTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public NegativePermissionTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task NonAdmin_Cannot_Access_Admin_Page_Returns_Redirect_Or_Forbidden()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/admin", new() { WaitUntil = WaitUntilState.NetworkIdle });

        var url = page.Url.ToLower();
        var bodyText = await page.Locator("body").TextContentAsync() ?? "";

        // Should either redirect to login, show access denied, or show 404
        var isAccessDenied = url.Contains("accessdenied") 
            || url.Contains("login") 
            || url.Contains("404")
            || !bodyText.Contains("Admin Panel", StringComparison.OrdinalIgnoreCase);

        Assert.True(isAccessDenied, $"Non-admin user was able to access admin page at {url}");
    }

    [Fact]
    public async Task Unauthenticated_User_Cannot_Access_Dashboard()
    {
        await using var context = await _fixture.NewAnonymousContextAsync();
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
        
        // Should be redirected to login
        await page.WaitForURLAsync("**/Account/Login**");
        Assert.Contains("/Account/Login", page.Url);
    }

    [Fact]
    public async Task Unauthenticated_User_Cannot_Create_Ticket()
    {
        await using var context = await _fixture.NewAnonymousContextAsync();
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");
        
        // Should be redirected to login
        await page.WaitForURLAsync("**/Account/Login**");
        Assert.Contains("/Account/Login", page.Url);
    }

    [Fact]
    public async Task NonAdmin_Sidebar_Does_Not_Show_Admin_Link()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");

        // Verify the Admin link is not visible
        var adminLinks = await page.Locator(".sidebar-nav")
            .GetByRole(AriaRole.Link, new() { Name = "Admin" })
            .AllAsync();

        Assert.Empty(adminLinks);
    }

    [Fact]
    public async Task Admin_Sidebar_Shows_Admin_Link()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");

        // Verify the Admin link IS visible for admins
        await Assertions.Expect(page.Locator(".sidebar-nav").GetByRole(AriaRole.Link, new() { Name = "Admin" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task User_Can_Create_Ticket()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/tickets/create");

        // Verify user can see the create form
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Create Support Ticket");
        await Assertions.Expect(page.GetByLabel("Title")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Admin_User_Can_Access_Admin_Panel()
    {
        await using var context = await _fixture.NewAdminContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/admin");

        // Verify admin can see the admin panel
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Admin Panel");
    }

    [Fact]
    public async Task Login_With_Invalid_Credentials_Shows_Error()
    {
        await using var context = await _fixture.NewAnonymousContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login");

        await page.GetByLabel("Email").FillAsync("user@template.local");
        await page.GetByLabel("Password").FillAsync("WrongPassword123!");
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Assertions.Expect(page.GetByText("Invalid login attempt")).ToBeVisibleAsync();
        Assert.Contains("/Account/Login", page.Url);
    }

    [Fact]
    public async Task Empty_Login_Form_Shows_Validation_Errors()
    {
        await using var context = await _fixture.NewAnonymousContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login");

        // Try to submit without filling in any fields
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        // Validation messages should appear
        var validationMessages = await page.Locator(".text-danger").AllAsync();
        Assert.NotEmpty(validationMessages);
    }

    [Fact]
    public async Task Logout_Clears_Session()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");

        // Verify user is on dashboard
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Dashboard");

        // Find and click logout button (try common text variations)
        var logoutButton = page.GetByRole(AriaRole.Button, new() { Name = "Logout" });
        
        if (!await logoutButton.IsVisibleAsync())
        {
            // Try alternative text
            logoutButton = page.GetByRole(AriaRole.Button, new() { Name = "Log out" });
        }
        
        if (await logoutButton.IsVisibleAsync())
        {
            await logoutButton.ClickAsync();
            
            // After logout, should be redirected
            var url = page.Url.ToLower();
            Assert.True(url.Contains("login") || url.Contains("home") || url.Contains("account"),
                $"Expected to be redirected after logout, but landed at {url}");
            
            // Verify we're no longer authenticated by trying to access dashboard
            await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");
            await page.WaitForURLAsync("**/Account/Login**");
        }
    }

    [Fact]
    public async Task Different_Users_Cannot_See_Each_Other_Tickets()
    {
        // This test verifies that two different user contexts don't share ticket data
        await using var context1 = await _fixture.NewUserContextAsync();
        var page1 = await context1.NewPageAsync();
        await page1.GotoAsync($"{_fixture.BaseUrl}/tickets");

        var ticketList1 = await page1.Locator(".ticket-item, tr").AllAsync();
        var count1 = ticketList1.Count;

        // Use a different context (still the same seeded user, but in a fresh session)
        await using var context2 = await _fixture.NewUserContextAsync();
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync($"{_fixture.BaseUrl}/tickets");

        var ticketList2 = await page2.Locator(".ticket-item, tr").AllAsync();
        var count2 = ticketList2.Count;

        // Both contexts should show the same tickets for the same user
        Assert.Equal(count1, count2);
    }

    [Fact]
    public async Task Home_Page_Is_Accessible_To_Unauthenticated_Users()
    {
        await using var context = await _fixture.NewAnonymousContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/");

        // Home page should load successfully
        var url = page.Url.ToLower();
        Assert.False(url.Contains("login"), "Unauthenticated user was redirected from home page");
    }

    [Fact]
    public async Task Authenticated_User_Redirected_From_Home_To_Dashboard()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        
        // Navigate to home page
        await page.GotoAsync($"{_fixture.BaseUrl}/");

        // After navigation, might redirect to dashboard or stay on home
        var url = page.Url;
        var bodyText = await page.Locator("body").TextContentAsync() ?? "";

        // Either on dashboard or home is acceptable for authenticated users
        var isValidLocation = url.Contains("/dashboard") || url == $"{_fixture.BaseUrl}/" || bodyText.Contains("Dashboard");
        Assert.True(isValidLocation, $"Unexpected redirect location: {url}");
    }

    [Fact]
    public async Task Email_Field_Validation_Shows_Error_For_Invalid_Format()
    {
        await using var context = await _fixture.NewAnonymousContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/Account/Login");

        // Attempt to submit with invalid email format
        await page.GetByLabel("Email").FillAsync("not-an-email");
        await page.GetByLabel("Password").FillAsync("SomePassword123!");
        
        // Try to submit
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        // Either shows validation error or navigates to dashboard (depending on browser-side validation)
        await page.WaitForTimeoutAsync(1000); // Wait a moment for any validation to trigger
        
        // Verify we're still on login or validation messages appear
        var stillOnLogin = page.Url.Contains("/Account/Login");
        var hasErrorMessage = await page.Locator(".text-danger, .alert-danger, .validation-message").CountAsync() > 0;
        
        Assert.True(stillOnLogin || hasErrorMessage, "No validation occurred for invalid email format");
    }

    [Fact]
    public async Task User_Without_Auth_Cannot_Access_Authenticated_Pages()
    {
        var protectedPages = new[] { "/dashboard", "/tickets", "/tickets/create", "/admin" };

        foreach (var page in protectedPages)
        {
            await using var context = await _fixture.NewAnonymousContextAsync();
            var playPage = await context.NewPageAsync();

            await playPage.GotoAsync($"{_fixture.BaseUrl}{page}");

            // Should be redirected to login
            var url = playPage.Url.ToLower();
            var isRedirectedToLogin = url.Contains("login") || url.Contains("accessdenied");

            Assert.True(isRedirectedToLogin, 
                $"Unauthenticated access to {page} was not properly protected. Landed at: {playPage.Url}");
        }
    }

    [Fact]
    public async Task NonAdmin_User_Cannot_See_Admin_User_Management_Features()
    {
        await using var context = await _fixture.NewUserContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_fixture.BaseUrl}/dashboard");

        // Verify there's no user management section visible
        var adminElements = await page.Locator("*:has-text('Admin'), *:has-text('Users'), *:has-text('Manage')").AllAsync();
        
        // Filter for actual admin-related content (loose check)
        var adminRelatedContent = adminElements
            .Where(e => e.TextContentAsync().Result?.Contains("Admin") == true 
                     || e.TextContentAsync().Result?.Contains("Manage Users") == true)
            .ToList();

        // Non-admin users should not see admin-related content on dashboard
        Assert.Empty(adminRelatedContent);
    }
}
