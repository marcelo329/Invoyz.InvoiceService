using ErrorOr;
using FluentValidation;
using MediatR;

namespace Invoyz.InvoiceService.Application;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var errors = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .ToList();

        return errors.Count == 0
            ? await next(cancellationToken)
            : BuildFailureResponse(errors);
    }

    private static TResponse BuildFailureResponse(List<Error> errors)
    {
        if (typeof(TResponse) == typeof(Error?))
            return (TResponse)(object)errors[0];

        // ErrorOr<T> defines an implicit conversion from List<Error>
        var conversion = typeof(TResponse).GetMethod("op_Implicit", [typeof(List<Error>)])
            ?? throw new InvalidOperationException($"Cannot build a validation response for {typeof(TResponse)}.");

        return (TResponse)conversion.Invoke(null, [errors])!;
    }
}
