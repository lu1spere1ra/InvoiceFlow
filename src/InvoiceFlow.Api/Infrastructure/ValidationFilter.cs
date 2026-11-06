using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiceFlow.Api.Infrastructure;

/// <summary>
/// Runs the FluentValidation validator registered for each action argument, if any, and short-circuits
/// with a 400 ValidationProblemDetails. This replaces a MediatR validation pipeline behaviour.
/// </summary>
internal sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null ||
                services.GetService(typeof(IValidator<>).MakeGenericType(argument.GetType())) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Instance = context.HttpContext.Request.Path,
                });
                return;
            }
        }

        await next();
    }
}
