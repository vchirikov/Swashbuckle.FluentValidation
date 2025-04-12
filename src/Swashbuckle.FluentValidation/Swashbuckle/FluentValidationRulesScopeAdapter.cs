// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.FluentValidation;

/// <summary>
/// Creates service from service provider with desired lifestyle.
/// </summary>
public class FluentValidationRulesScopeAdapter : ISchemaFilter, IDisposable
{
    private readonly FluentValidationRules _fluentValidationRules;
    private bool _isDisposed;
    private IServiceScope? _serviceScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationRulesScopeAdapter"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="serviceLifetime"><see cref="ServiceLifetime"/> to use.</param>
    public FluentValidationRulesScopeAdapter(IServiceProvider serviceProvider, ServiceLifetime serviceLifetime)
    {
        // Hack with the scope mismatch.
        if (serviceLifetime is ServiceLifetime.Scoped or ServiceLifetime.Transient) {
            _serviceScope = serviceProvider.CreateScope();
            serviceProvider = _serviceScope.ServiceProvider;
        }

        _fluentValidationRules = serviceProvider.GetService<FluentValidationRules>();

        if (_fluentValidationRules == null) {
            var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(FluentValidationRulesScopeAdapter));
            logger?.LogWarning($"{nameof(FluentValidationRules)} should be registered in services. Hint: Use registration method '{nameof(ServiceCollectionExtensions.AddFluentValidationRulesToSwagger)}'");
        }

        // Last chance to create filter
        _fluentValidationRules ??= ActivatorUtilities.CreateInstance<FluentValidationRules>(serviceProvider);
    }

    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        _fluentValidationRules.Apply(schema, context);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed) {
            if (disposing) {
                _serviceScope?.Dispose();
            }
            _serviceScope = null;
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Creates service from service provider with desired lifestyle.
/// </summary>
public class FluentValidationOperationFilterScopeAdapter : IOperationFilter, IDisposable
{
    private readonly FluentValidationOperationFilter _fluentValidationRules;
    private bool _isDisposed;
    private IServiceScope? _serviceScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationOperationFilterScopeAdapter"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="serviceLifetime"><see cref="ServiceLifetime"/> to use.</param>
    public FluentValidationOperationFilterScopeAdapter(IServiceProvider serviceProvider, ServiceLifetime serviceLifetime)
    {
        // Hack with the scope mismatch.
        if (serviceLifetime is ServiceLifetime.Scoped or ServiceLifetime.Transient) {
            _serviceScope = serviceProvider.CreateScope();
            serviceProvider = _serviceScope.ServiceProvider;
        }

        _fluentValidationRules = serviceProvider.GetService<FluentValidationOperationFilter>();

        if (_fluentValidationRules == null) {
            var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(FluentValidationRulesScopeAdapter));
            logger?.LogWarning($"{nameof(FluentValidationOperationFilter)} should be registered in services. Hint: Use registration method '{nameof(ServiceCollectionExtensions.AddFluentValidationRulesToSwagger)}'");
        }

        // Last chance to create filter
        _fluentValidationRules ??= ActivatorUtilities.CreateInstance<FluentValidationOperationFilter>(serviceProvider);
    }

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        _fluentValidationRules.Apply(operation, context);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed) {
            if (disposing) {
                _serviceScope?.Dispose();
            }
            _serviceScope = null;
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Creates service from service provider with desired lifestyle.
/// </summary>
public class DocumentFilterScopeAdapter<TDocumentFilter> : IDocumentFilter, IDisposable
    where TDocumentFilter : IDocumentFilter
{
    private readonly IDocumentFilter _documentFilter;
    private bool _isDisposed;
    private IServiceScope? _serviceScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentFilterScopeAdapter{TDocumentFilter}"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
    /// <param name="serviceLifetime"><see cref="ServiceLifetime"/> to use.</param>
    public DocumentFilterScopeAdapter(IServiceProvider serviceProvider, ServiceLifetime serviceLifetime)
    {
        // Hack with the scope mismatch.
        if (serviceLifetime == ServiceLifetime.Scoped || serviceLifetime == ServiceLifetime.Transient) {
            _serviceScope = serviceProvider.CreateScope();
            serviceProvider = _serviceScope.ServiceProvider;
        }

        _documentFilter = serviceProvider.GetService<TDocumentFilter>();

        if (_documentFilter == null) {
            var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
            logger?.LogWarning($"{nameof(TDocumentFilter)} should be registered in services. Hint: Use registration method '{nameof(ServiceCollectionExtensions.AddFluentValidationRulesToSwagger)}'");
        }

        // Last chance to create filter
        _documentFilter ??= ActivatorUtilities.CreateInstance<TDocumentFilter>(serviceProvider);
    }

    /// <inheritdoc />
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        _documentFilter.Apply(swaggerDoc, context);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed) {
            if (disposing) {
                _serviceScope?.Dispose();
            }
            _serviceScope = null;
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}