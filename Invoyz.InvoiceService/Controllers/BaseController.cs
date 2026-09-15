using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Invoyz.InvoiceService.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public abstract class BaseController(IMediator mediator) : ControllerBase
{

    public virtual async Task<IActionResult> PostAsync<T>(T command, CancellationToken cancellationToken) where T : IRequest<ErrorOr<Guid>>
    {
        var result = await mediator.Send(command, cancellationToken);
        return DispatchCreationResult(result);
    }
    public virtual async Task<IActionResult> PutAsync<T>(T command, CancellationToken cancellationToken) where T : IRequest<Error?>
    {
        var result = await mediator.Send(command, cancellationToken);
        return DispatchCommandResult(result);
    }
    public virtual async Task<IActionResult> DeleteAsync<T>(T command, CancellationToken cancellationToken) where T : IRequest<Error?>
    {
        var result = await mediator.Send(command, cancellationToken);
        return DispatchCommandResult(result);
    }
    public virtual async Task<IActionResult> GetAsync<T,T1>(T query,CancellationToken cancellationToken) where T: IRequest<IReadOnlyCollection<T1>>
    {
        var result = await mediator.Send(query, cancellationToken);
        return Ok(result);
    }
    public virtual async Task<IActionResult> GetByIdAsync<T,T1>(T query, CancellationToken cancellationToken) where T : IRequest<ErrorOr<T1>>
    {
        var result = await mediator.Send(query, cancellationToken);
        return DispatchQueryResult(result);
    }

    private IActionResult DispatchQueryResult<T>(ErrorOr<T> result)
        => result.IsError ?
        DispatchErrorMessage(result.Errors) :
        Ok(result.Value);

    private IActionResult DispatchCommandResult(Error? result)
        => result.HasValue ?
        DispatchErrorMessage(result.Value) :
        Ok();
    private IActionResult DispatchCreationResult(ErrorOr<Guid> result)
        => result.IsError ?
        DispatchErrorMessage(result.Errors) :
        CreatedAtAction("Created", result.Value);
    private IActionResult DispatchErrorMessage(params List<Error> errors)
    {
        if (errors.Count == 1)
            return Problem(statusCode: GetErrorStatusCode(errors.First()), detail: errors.First().Code);

        var dicErrors = new Dictionary<string, object>();

        foreach (var error in errors)
            dicErrors.Add(GetErrorStatusCode(error).ToString(), error.Code);

        return Problem(extensions: dicErrors!);

        int GetErrorStatusCode(Error error)
            =>  error.Type switch
            {
                ErrorType.Conflict => 409,
                ErrorType.Validation => 400,
                _ => 500
            };
    }
}
