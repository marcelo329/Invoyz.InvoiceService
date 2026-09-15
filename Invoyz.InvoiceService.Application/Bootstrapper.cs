using FluentValidation;
using Invoyz.InvoiceService.Application.CQRS.Customers.Commands.Validators;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Invoyz.InvoiceService.Application;

public static class Bootstrapper
{
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateCustomerCommandValidator>();
        services.AddValidatorsFromAssemblyContaining<UpdateCustomerCommandValidator>();

        return services;
    }
}
