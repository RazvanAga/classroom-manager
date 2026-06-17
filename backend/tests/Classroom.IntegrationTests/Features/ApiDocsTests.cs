using System.Net;
using Classroom.IntegrationTests.Infrastructure;

namespace Classroom.IntegrationTests.Features;

public class ApiDocsTests(ClassroomApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task OpenApi_document_is_served()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/auth/login", body);
    }

    [Fact]
    public async Task Scalar_reference_ui_is_served()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
