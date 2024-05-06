using FluentValidation;
using MediatR;
using Mehedi.Application.SharedKernel.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace KYC.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Adds command handlers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddApplications(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return services
            .AddValidatorsFromAssembly(assembly)
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly)
            .AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)));
    }
}
