using System.Net.Http.Json;
using System.Text.Json;
using Classroom.Domain.Common;
using Classroom.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Classroom.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase(ClassroomApiFactory factory)
{
    protected ClassroomApiFactory Factory { get; } = factory;

    /// <summary>A client that persists cookies (auth + antiforgery) across requests, like a browser.</summary>
    protected HttpClient CreateClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    /// <summary>
    /// Creates an extra teacher directly via Identity (no public registration exists) so tests can
    /// exercise cross-tenant denial and Owner/Collaborator rules with a second principal.
    /// Returns the new teacher's id.
    /// </summary>
    protected async Task<Guid> CreateTeacherAsync(string email, string password = "Passw0rd!", string? displayName = null)
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var teacher = new ApplicationUser
        {
            Id = EntityId.New(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName ?? email,
        };

        var result = await users.CreateAsync(teacher, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Failed to create teacher '{email}': {errors}");
        }

        return teacher.Id;
    }

    /// <summary>
    /// Fetches an antiforgery token (which also drops the antiforgery cookie into the client's jar)
    /// and returns the header value to attach to a mutating request.
    /// </summary>
    protected static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/antiforgery/token");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("token").GetString()!;
    }

    /// <summary>Logs in as the seeded teacher on the given client; leaves the auth cookie set.</summary>
    protected static Task LoginAsSeededTeacherAsync(HttpClient client) =>
        LoginAsync(client, ClassroomApiFactory.SeedEmail, ClassroomApiFactory.SeedPassword);

    /// <summary>Logs in as the given teacher on the given client; leaves the auth cookie set.</summary>
    protected static async Task LoginAsync(HttpClient client, string email, string password)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password }),
        };
        request.Headers.Add("X-XSRF-TOKEN", token);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Sends a mutating request with a fresh antiforgery token attached.</summary>
    protected static async Task<HttpResponseMessage> SendWithTokenAsync(
        HttpClient client, HttpMethod method, string uri, object? body = null)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-XSRF-TOKEN", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }
}
