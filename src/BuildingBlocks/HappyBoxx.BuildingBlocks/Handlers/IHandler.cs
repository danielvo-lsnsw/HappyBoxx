using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HappyBoxx.BuildingBlocks.Handlers;

/// <summary>Marker for use-case handlers; implementations are registered as scoped services automatically.</summary>
public interface IHandler;

internal static class HandlerExtensions
{
    internal static IServiceCollection AddHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IHandler)));

        foreach (var type in handlerTypes)
        {
            services.TryAddScoped(type);
        }

        return services;
    }
}
