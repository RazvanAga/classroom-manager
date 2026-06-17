using FluentValidation;

namespace Classroom.Api.Common.Validation;

/// <summary>
/// Endpoint filter that runs the registered FluentValidation validator for <typeparamref name="T"/>
/// against the first argument of that type, returning an RFC 9457 ValidationProblemDetails on failure.
/// </summary>
public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var model = context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
        {
            return Results.Problem(
                title: "Invalid request",
                detail: $"Expected a body of type {typeof(T).Name}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await validator.ValidateAsync(model);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
