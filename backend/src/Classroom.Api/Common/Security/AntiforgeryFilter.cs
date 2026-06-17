using Microsoft.AspNetCore.Antiforgery;

namespace Classroom.Api.Common.Security;

/// <summary>
/// Validates the antiforgery token on a mutating endpoint, returning an RFC 9457 ProblemDetails
/// (400) when it is missing or invalid. The double-submit token is issued by
/// <c>GET /api/antiforgery/token</c>; the SPA echoes it back in the configured header.
/// </summary>
public class AntiforgeryFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Problem(
                title: "Antiforgery validation failed",
                detail: "A valid antiforgery token is required. Fetch one from /api/antiforgery/token.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }
}
