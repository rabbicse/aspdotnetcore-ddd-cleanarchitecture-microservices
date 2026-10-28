using FluentValidation;
using KYC.Application.Behaviors;
using Mehedi.Application.SharedKernel.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KYC.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application layer: MediatR handlers, FluentValidation validators and the
    /// logging and validation pipeline behaviors (logging wraps validation).
    /// </summary>
    /// <remarks>
    /// Infrastructure projects call <c>AddMediatR</c> themselves to register their own notification
    /// handlers; MediatR merges those registrations, so this method does not need to know about them.
    /// </remarks>
    public static IServiceCollection AddApplications(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.TryAddSingleton(TimeProvider.System);

        return services
            .AddValidatorsFromAssembly(assembly)
            .AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
    }
}
