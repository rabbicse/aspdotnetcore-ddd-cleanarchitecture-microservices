using FluentValidation;
using Mehedi.Application.SharedKernel.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ordering.Application.Behaviors;

namespace Ordering.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application layer: MediatR handlers (commands, queries, domain event handlers),
    /// FluentValidation validators, and the logging → validation pipeline.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
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
