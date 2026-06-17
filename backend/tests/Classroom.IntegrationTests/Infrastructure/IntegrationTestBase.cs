using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Classroom.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase(ClassroomApiFactory factory)
{
    protected ClassroomApiFactory Factory { get; } = factory;

    /// <summary>A client that persists cookies (auth + antiforgery) across requests, like a browser.</summary>
    protected HttpClient CreateClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

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
    protected static async Task LoginAsSeededTeacherAsync(HttpClient client)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email = ClassroomApiFactory.SeedEmail,
                password = ClassroomApiFactory.SeedPassword,
            }),
        };
        request.Headers.Add("X-XSRF-TOKEN", token);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
