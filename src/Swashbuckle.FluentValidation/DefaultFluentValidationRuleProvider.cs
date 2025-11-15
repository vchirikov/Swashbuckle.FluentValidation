// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation.Validators;
using OpenApi.Core;
using OpenApi.FluentValidation;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Swashbuckle.FluentValidation;

/// <summary>
/// Default rule provider.
/// </summary>
public class DefaultFluentValidationRuleProvider : IFluentValidationRuleProvider<IOpenApiSchema>
{
    private readonly IOptions<SchemaGenerationOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultFluentValidationRuleProvider"/> class.
    /// </summary>
    /// <param name="options">Schema generation options.</param>
    public DefaultFluentValidationRuleProvider(IOptions<SchemaGenerationOptions>? options = null)
    {
        _options = options ?? new OptionsWrapper<SchemaGenerationOptions>(new SchemaGenerationOptions());
    }

    /// <inheritdoc />
    public IEnumerable<IFluentValidationRule<IOpenApiSchema>> GetRules()
    {
        yield return new FluentValidationRule("Required")
            .WithCondition(validator => validator is INotNullValidator || validator is INotEmptyValidator)
            .WithApply(context => {
                if (context.Schema.Required != null) {
                    context.Schema.Required.Add(context.PropertyKey);
                    if (context.Property is OpenApiSchema schema) {
                        schema.Type &= ~JsonSchemaType.Null;
                    }
                }
            });

        yield return new FluentValidationRule("NotEmpty")
            .WithCondition(validator => validator is INotEmptyValidator)
            .WithApply(context => {
                if (context.Property is OpenApiSchema propertySchema && propertySchema.Type != null) {
                    if (propertySchema.Type.Value.HasFlag(JsonSchemaType.String)) {
                        propertySchema.MinLength = propertySchema.MinLength is null
                            ? 1
                            : Math.Max(propertySchema.MinLength.Value, 1);

                        if (_options.Value.SetNotNullableIfMinLengthGreaterThanZero) {
                            propertySchema.Type &= ~JsonSchemaType.Null;
                        }

                        if (_options.Value.SetNotNullableIfMinimumGreaterThanZero) {
                            propertySchema.Type &= ~JsonSchemaType.Null;
                        }
                    }

                    if (propertySchema.Type.Value.HasFlag(JsonSchemaType.Array)) {
                        propertySchema.MinItems = propertySchema.MinItems is null
                            ? 1
                            : Math.Max(propertySchema.MinItems.Value, 1);

                        if (_options.Value.SetNotNullableIfMinLengthGreaterThanZero) {
                            propertySchema.Type &= ~JsonSchemaType.Null;
                        }

                        if (_options.Value.SetNotNullableIfMinimumGreaterThanZero) {
                            propertySchema.Type &= ~JsonSchemaType.Null;
                        }
                    }
                }
            });

        yield return new FluentValidationRule("Length")
            .WithCondition(validator => validator is ILengthValidator)
            .WithApply(context => {
                var lengthValidator = (ILengthValidator)context.PropertyValidator;
                if (context.Property is OpenApiSchema { Type: not null } propertySchema) {
                    if (propertySchema.Type.Value.HasFlag(JsonSchemaType.Array)) {
                        if (lengthValidator.Max > 0) {
                            propertySchema.MaxItems = propertySchema.MaxItems.HasValue
                                ? Math.Min(propertySchema.MaxItems.Value, lengthValidator.Max)
                                : lengthValidator.Max;
                        }

                        if (lengthValidator.Min > 0) {
                            propertySchema.MinItems = propertySchema.MinItems.HasValue
                                ? Math.Max(propertySchema.MinItems.Value, lengthValidator.Min)
                                : lengthValidator.Min;

                            if (_options.Value.SetNotNullableIfMinLengthGreaterThanZero) {
                                propertySchema.Type &= ~JsonSchemaType.Null;
                            }
                        }
                    }
                    else {
                        if (lengthValidator.Max > 0) {
                            propertySchema.MaxLength = propertySchema.MaxLength.HasValue
                                ? Math.Min(propertySchema.MaxLength.Value, lengthValidator.Max)
                                : lengthValidator.Max;
                        }

                        if (lengthValidator.Min > 0) {
                            propertySchema.MinLength = propertySchema.MinLength.HasValue
                                ? Math.Max(propertySchema.MinLength.Value, lengthValidator.Min)
                                : lengthValidator.Min;

                            if (_options.Value.SetNotNullableIfMinLengthGreaterThanZero) {
                                propertySchema.Type &= ~JsonSchemaType.Null;
                            }
                        }
                    }
                }
            });

        yield return new FluentValidationRule("Pattern")
            .WithCondition(validator => validator is IRegularExpressionValidator)
            .WithApply(context => {
                var regularExpressionValidator = (IRegularExpressionValidator)context.PropertyValidator;
                if (context.Property is OpenApiSchema schemaProperty) {
                    if (_options.Value.UseAllOfForMultipleRules) {
                        if (schemaProperty.AllOf != null && (schemaProperty.Pattern != null ||
                                                             schemaProperty.AllOf.Any(schema =>
                                                                 schema.Pattern != null))) {
                            if (schemaProperty.AllOf.All(schema => schema.Pattern == null)) {
                                // Add first pattern as AllOf
                                schemaProperty.AllOf.Add(new OpenApiSchema() {
                                    Pattern = schemaProperty.Pattern,
                                });
                            }

                            // Add another pattern as AllOf
                            schemaProperty.AllOf.Add(new OpenApiSchema() {
                                Pattern = regularExpressionValidator.Expression,
                            });

                            schemaProperty.Pattern = null;
                        }
                        else {
                            // First and only pattern
                            schemaProperty.Pattern = regularExpressionValidator.Expression;
                        }
                    }
                    else {
                        // Set new pattern
                        schemaProperty.Pattern = regularExpressionValidator.Expression;
                    }
                }
            });

        yield return new FluentValidationRule("EMail")
            .WithCondition(propertyValidator => propertyValidator.GetType().Name.Contains("EmailValidator"))
            .WithApply(context => {
                if (context.Property is OpenApiSchema schemaProperty) {
                    schemaProperty.Format = "email";
                }
            });

        yield return new FluentValidationRule("Comparison")
            .WithCondition(validator => validator is IComparisonValidator)
            .WithApply(context => {
                var comparisonValidator = (IComparisonValidator)context.PropertyValidator;
                if (comparisonValidator.ValueToCompare.IsNumeric()) {
                    var valueToCompare = comparisonValidator.ValueToCompare.NumericToDecimal();
                    if (context.Property is OpenApiSchema propertySchema) {
                        if (comparisonValidator.Comparison is Comparison.GreaterThanOrEqual or Comparison.GreaterThan) {
                            propertySchema.Minimum = propertySchema.Minimum is null || !decimal.TryParse(
                                propertySchema.Minimum, CultureInfo.InvariantCulture,
                                out decimal currentMin)
                                ? valueToCompare.ToString(CultureInfo.InvariantCulture)
                                : Math.Max(currentMin, valueToCompare).ToString(CultureInfo.InvariantCulture);

                            if (_options.Value.SetNotNullableIfMinimumGreaterThanZero && valueToCompare > 0) {
                                propertySchema.Type &= ~JsonSchemaType.Null;
                            }

                            if (comparisonValidator.Comparison is Comparison.GreaterThan) {
                                // in openapi 3.0 this should be "true", but in 3.1 it should be used instead .Minimum prop 
                                propertySchema.ExclusiveMinimum ??= propertySchema.Minimum;
                            }
                        }
                        else if (comparisonValidator.Comparison is Comparison.LessThanOrEqual or Comparison.LessThan) {
                            propertySchema.Maximum = propertySchema.Maximum is null || !decimal.TryParse(
                                propertySchema.Maximum, CultureInfo.InvariantCulture,
                                out decimal currentMax)
                                ? valueToCompare.ToString(CultureInfo.InvariantCulture)
                                : Math.Min(currentMax, valueToCompare).ToString(CultureInfo.InvariantCulture);

                            if (comparisonValidator.Comparison is Comparison.LessThan) {
                                // in openapi 3.0 this should be "true", but in 3.1 it should be used instead .Minimum prop 
                                propertySchema.ExclusiveMaximum ??= propertySchema.Maximum;
                            }
                        }
                    }
                }
            });

        yield return new FluentValidationRule("Between")
            .WithCondition(validator => validator is IBetweenValidator)
            .WithApply(context => {
                var betweenValidator = (IBetweenValidator)context.PropertyValidator;
                if (context.Property is OpenApiSchema propertySchema) {
                    //OpenApi: date-time has not support range validations see: https://github.com/json-schema-org/json-schema-spec/issues/116

                    if (betweenValidator.From.IsNumeric()) {
                        decimal min = betweenValidator.From.NumericToDecimal();
                        propertySchema.Minimum = propertySchema.Minimum is null || !decimal.TryParse(
                            propertySchema.Minimum, CultureInfo.InvariantCulture,
                            out decimal currentMin)
                            ? min.ToString(CultureInfo.InvariantCulture)
                            : Math.Max(currentMin, min).ToString(CultureInfo.InvariantCulture);

                        if (_options.Value.SetNotNullableIfMinimumGreaterThanZero && min > 0) {
                            propertySchema.Type &= ~JsonSchemaType.Null;
                        }

                        if (string.Equals(betweenValidator.Name, "ExclusiveBetweenValidator",
                                StringComparison.Ordinal)) {
                            // in openapi 3.0 this should be "true", but in 3.1 it should be used instead .Minimum prop 
                            propertySchema.ExclusiveMinimum ??= propertySchema.Minimum;
                        }
                    }

                    if (betweenValidator.To.IsNumeric()) {
                        decimal max = betweenValidator.To.NumericToDecimal();
                        propertySchema.Maximum = propertySchema.Maximum is null || !decimal.TryParse(
                            propertySchema.Maximum, CultureInfo.InvariantCulture,
                            out decimal currentMax)
                            ? max.ToString(CultureInfo.InvariantCulture)
                            : Math.Min(currentMax, max).ToString(CultureInfo.InvariantCulture);

                        if (string.Equals(betweenValidator.Name, "ExclusiveBetweenValidator",
                                StringComparison.Ordinal)) {
                            // in openapi 3.0 this should be "true", but in 3.1 it should be used instead .Minimum prop 
                            propertySchema.ExclusiveMaximum ??= propertySchema.Maximum;
                        }
                    }
                }
            });
    }
}