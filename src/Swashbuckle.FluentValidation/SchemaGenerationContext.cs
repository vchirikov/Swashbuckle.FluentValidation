// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation.Validators;
using OpenApi.FluentValidation;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.FluentValidation;

/// <summary>
/// Schema generation context.
/// </summary>
public record SchemaGenerationContext : ISchemaGenerationContext<IOpenApiSchema>
{
    /// <inheritdoc/>
    public Type SchemaType { get; }

    /// <inheritdoc />
    IReadOnlyList<IFluentValidationRule> ISchemaGenerationContext.Rules => Rules;

    /// <inheritdoc/>
    public IReadOnlyList<IFluentValidationRule<IOpenApiSchema>> Rules { get; }

    /// <inheritdoc/>
    public ISchemaGenerationOptions SchemaGenerationOptions { get; }

    /// <inheritdoc/>
    public IOpenApiSchema Schema { get; }

    /// <inheritdoc/>
    public ISchemaProvider<IOpenApiSchema> SchemaProvider { get; }

    /// <inheritdoc/>
    public IEnumerable<string> Properties => Schema.Properties?.Keys ?? Array.Empty<string>();

    /// <summary>
    /// Gets Swashbuckle <see cref="ISchemaGenerator"/>.
    /// </summary>
    internal ISchemaGenerator SchemaGenerator { get; }

    /// <summary>
    /// Gets Swashbuckle <see cref="SchemaRepository"/>.
    /// </summary>
    internal SchemaRepository SchemaRepository { get; }

    /// <inheritdoc />
    public ISchemaGenerationContext<IOpenApiSchema> With(IOpenApiSchema schema)
    {
        return new SchemaGenerationContext(
            schemaRepository: SchemaRepository,
            schemaGenerator: SchemaGenerator,
            schema: schema,
            schemaType: SchemaType,
            rules: Rules,
            schemaGenerationOptions: SchemaGenerationOptions,
            schemaProvider: SchemaProvider);
    }

    /// <inheritdoc />
    public IRuleContext<IOpenApiSchema> Create(
        string schemaPropertyName,
        ValidationRuleContext validationRuleContext,
        IPropertyValidator propertyValidator)
    {
        return new OpenApiRuleContext(Schema, schemaPropertyName, validationRuleContext, propertyValidator);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaGenerationContext"/> class.
    /// </summary>
    public SchemaGenerationContext(
        SchemaRepository schemaRepository,
        ISchemaGenerator schemaGenerator,

        IOpenApiSchema schema,
        Type schemaType,

        IReadOnlyList<IFluentValidationRule<IOpenApiSchema>> rules,
        ISchemaGenerationOptions schemaGenerationOptions,
        ISchemaProvider<IOpenApiSchema>? schemaProvider = null)
    {
        SchemaRepository = schemaRepository;
        SchemaGenerator = schemaGenerator;

        Schema = schema;
        SchemaType = schemaType;
        Rules = rules;
        SchemaGenerationOptions = schemaGenerationOptions;

        SchemaProvider = schemaProvider ?? new SwashbuckleSchemaProvider(
            schemaRepository,
            schemaGenerator,
            schemaGenerationOptions.SchemaIdSelector);
    }
}