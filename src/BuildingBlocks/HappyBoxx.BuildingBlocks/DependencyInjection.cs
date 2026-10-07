using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation;
using HappyBoxx.BuildingBlocks.Endpoints;
using HappyBoxx.BuildingBlocks.Exceptions;
using HappyBoxx.BuildingBlocks.Handlers;
using HappyBoxx.BuildingBlocks.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HappyBoxx.BuildingBlocks;

public static class DependencyInjection
{
    /// <summary>Registers the common API pipeline pieces plus the endpoints, handlers and validators found in <paramref name="assembly"/>.</summary>
    public static IServiceCollection AddBuildingBlocks(this IServiceCollection services, Assembly assembly)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AuditableEntityInterceptor>();

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddHandlers(assembly);
        services.AddEndpoints(assembly);

        return services;
    }
}
