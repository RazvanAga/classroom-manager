using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features.Auth;

public class AuthEndpointsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_then_Me_returns_the_seeded_teacher_name()
    {
        var client = CreateClient();

        await LoginAsSeededTeacherAsync(client);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        using var json = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal(ClassroomApiFactory.SeedDisplayName, json.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(ClassroomApiFactory.SeedEmail, json.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_without_authentication_is_rejected_with_401()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_clears_the_session_so_Me_is_unauthorized_afterward()
    {
        var client = CreateClient();
        await LoginAsSeededTeacherAsync(client);

        var token = await GetAntiforgeryTokenAsync(client);
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add("X-XSRF-TOKEN", token);
        var logoutResponse = await client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Login_without_an_antiforgery_token_is_rejected_with_ProblemDetails()
    {
        var client = CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = ClassroomApiFactory.SeedEmail,
            password = ClassroomApiFactory.SeedPassword,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_ProblemDetails()
    {
        var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = ClassroomApiFactory.SeedEmail, password = "wrong-password" }),
        };
        request.Headers.Add("X-XSRF-TOKEN", token);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_with_invalid_email_returns_validation_ProblemDetails()
    {
        var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = "not-an-email", password = "" }),
        };
        request.Headers.Add("X-XSRF-TOKEN", token);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Email", out _) || errors.TryGetProperty("Password", out _));
    }
}
