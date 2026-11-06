using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InvoiceFlow.Api.IntegrationTests.Infrastructure;
using InvoiceFlow.Application.Customers;

namespace InvoiceFlow.Api.IntegrationTests;

public sealed class AuthApiTests(ApiFactoryFixture fixture) : IntegrationTest(fixture)
{
    private const string Cookie = "invoiceflow_refresh";

    [Fact]
    public async Task Endpoints_require_authentication()
    {
        var response = await AnonymousClient().GetAsync("/api/invoices", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Api_docs_and_health_are_public()
    {
        var client = AnonymousClient();

        (await client.GetAsync("/openapi/v1.json", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/health", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var response = await AnonymousClient().PostAsJsonAsync(
            "/api/auth/login", new { email = "admin@invoiceflow.local", password = "nope-nope" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_returns_user_roles_and_sets_an_http_only_refresh_cookie()
    {
        var response = await Login("accountant@invoiceflow.local", "Accountant!2026");

        var session = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        session.GetProperty("user").GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ShouldBe(["Accountant"]);

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(Cookie, StringComparison.Ordinal));
        cookie.ShouldContain("httponly", Case.Insensitive);
        cookie.ShouldContain("path=/api/auth", Case.Insensitive);
        cookie.ShouldContain("samesite=strict", Case.Insensitive);
    }

    [Theory]
    [InlineData("viewer@invoiceflow.local", "Viewer!2026", HttpStatusCode.Forbidden)]
    [InlineData("accountant@invoiceflow.local", "Accountant!2026", HttpStatusCode.Created)]
    public async Task Only_writers_can_change_data(string email, string password, HttpStatusCode expected)
    {
        var client = await ClientForAsync(email, password);
        var nif = email.StartsWith("viewer", StringComparison.Ordinal) ? "508890128" : "234567899";

        var response = await client.PostAsJsonAsync(
            "/api/customers", new CustomerInput("Cliente Teste", nif, "Rua 1", "1000-001", "Lisboa", "PT", null), Json, Ct);

        response.StatusCode.ShouldBe(expected);
    }

    [Fact]
    public async Task Viewers_can_read()
    {
        var viewer = await ClientForAsync("viewer@invoiceflow.local", "Viewer!2026");

        (await viewer.GetAsync("/api/invoices", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_reuse_revokes_every_session()
    {
        var first = RefreshCookieOf(await Login("viewer@invoiceflow.local", "Viewer!2026"));

        var rotated = await Refresh(first);
        rotated.StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = RefreshCookieOf(rotated);
        second.ShouldNotBe(first);

        // Replaying the first (already used) token is treated as theft...
        (await Refresh(first)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // ...so the legitimate, newer token stops working too.
        (await Refresh(second)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var token = RefreshCookieOf(await Login("viewer@invoiceflow.local", "Viewer!2026"));

        var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add("Cookie", $"{Cookie}={token}");
        (await AnonymousClient().SendAsync(logout, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await Refresh(token)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private Task<HttpResponseMessage> Login(string email, string password) =>
        AnonymousClient().PostAsJsonAsync("/api/auth/login", new { email, password }, Ct);

    private Task<HttpResponseMessage> Refresh(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{Cookie}={token}");
        return AnonymousClient().SendAsync(request, Ct);
    }

    private static string RefreshCookieOf(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(Cookie, StringComparison.Ordinal));
        return header.Split(';')[0][(Cookie.Length + 1)..];
    }
}
