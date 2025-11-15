// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using FluentValidation.Validators;
using OpenApi.FluentValidation;
using Microsoft.OpenApi;

namespace Swashbuckle.FluentValidation;

/// <summary>
/// RuleContext.
/// </summary>
public class OpenApiRuleContext : IRuleContext<IOpenApiSchema>
{
    /// <summary>
    /// Gets property name in schema.
    /// </summary>
    public string PropertyKey { get; }

    /// <summary>
    /// Gets property validator for property in schema.
    /// </summary>
    public IPropertyValidator PropertyValidator { get; }

    /// <summary>
    /// Gets OpenApi (swagger) schema.
    /// </summary>
    public IOpenApiSchema Schema { get; }

    /// <summary>
    /// Gets target property schema.
    /// </summary>
    public IOpenApiSchema Property
    {
        get
        {
            if (Schema.Properties == null ||
                !Schema.Properties.TryGetValue(PropertyKey, out IOpenApiSchema? schemaProperty)) {
                Type? schemaType = ValidationRuleInfo.GetReflectionContext()?.Type;
                throw new ApplicationException(
                    $"Schema for type '{schemaType}' does not contain property '{PropertyKey}'.\nRegister {typeof(INameResolver)} if name in type differs from name in json.");
            }

            return !ValidationRuleInfo.IsCollectionRule() ? schemaProperty : schemaProperty.Items;
        }
    }

    /// <summary>
    /// Gets <see cref="IValidationRule"/> with extended information.
    /// </summary>
    private ValidationRuleContext ValidationRuleInfo { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiRuleContext"/> class.
    /// </summary>
    /// <param name="schema">Swagger schema.</param>
    /// <param name="propertyKey">Property name.</param>
    /// <param name="validationRuleInfo">ValidationRuleInfo.</param>
    /// <param name="propertyValidator">Property validator.</param>
    public OpenApiRuleContext(
        IOpenApiSchema schema,
        string propertyKey,
        ValidationRuleContext validationRuleInfo,
        IPropertyValidator propertyValidator
    )
    {
        Schema = schema;
        PropertyKey = propertyKey;
        ValidationRuleInfo = validationRuleInfo;
        PropertyValidator = propertyValidator;
    }
}