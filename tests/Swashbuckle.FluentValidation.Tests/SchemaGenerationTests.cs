using System.Collections;
using System.Globalization;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.Validators;
using MicroElements.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.FluentValidation.Tests;

public partial class SchemaGenerationTests : UnitTestBase
{
    public class ComplexObject
    {
        public string TextProperty1 { get; set; }
        public string TextProperty2 { get; set; }
        public string TextProperty3 { get; set; }
    }

    public string TextProperty1 = nameof(ComplexObject.TextProperty1);
    public string TextProperty2 = nameof(ComplexObject.TextProperty2);
    public string TextProperty3 = nameof(ComplexObject.TextProperty3);

    public class ComplexObjectValidator : AbstractValidator<ComplexObject>
    {
        public ComplexObjectValidator()
        {
            RuleFor(x => x.TextProperty1).NotEmpty();
        }
    }

    [Fact]
    public void NotEmpty_Should_Set_MinLength()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)SchemaGenerator(new ComplexObjectValidator())
            .GenerateSchema(typeof(ComplexObject), schemaRepository);

        Assert.NotNull(referenceSchema.Reference);
        Assert.Equal("ComplexObject", referenceSchema.Reference.Id);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];

        Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Object));
        Assert.Equivalent(new string[] { TextProperty1, TextProperty2, TextProperty3, }, schema.Properties.Keys);

        Assert.Equal(1, schema.Properties[TextProperty1].MinLength);
    }

    public class Validator2 : AbstractValidator<ComplexObject>
    {
        public Validator2()
        {
            RuleFor(x => x.TextProperty1).NotEmpty().MaximumLength(64);
            RuleFor(x => x.TextProperty2).MaximumLength(64).NotEmpty();
            RuleFor(x => x.TextProperty3).NotNull().MaximumLength(64);
        }
    }

    [Fact]
    public void MaximumLength_ShouldNot_Override_NotEmpty()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)SchemaGenerator(new Validator2())
            .GenerateSchema(typeof(ComplexObject), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];

        Assert.Equal(1, schema.Properties[TextProperty1].MinLength);
        Assert.Equal(64, schema.Properties[TextProperty1].MaxLength);
        Assert.False(schema.Properties[TextProperty1].Type.Value.HasFlag(JsonSchemaType.Null));

        Assert.Equal(1, schema.Properties[TextProperty2].MinLength);
        Assert.Equal(64, schema.Properties[TextProperty2].MaxLength);
        Assert.False(schema.Properties[TextProperty2].Type.Value.HasFlag(JsonSchemaType.Null));
    }

    /// <summary>
    /// https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation/pull/67
    /// </summary>
    [Fact]
    public void MaximumLength_ShouldNot_Override_NotNull()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)SchemaGenerator(new Validator2())
            .GenerateSchema(typeof(ComplexObject), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];

        Assert.Equal(64, schema.Properties[TextProperty3].MaxLength);
        Assert.False(schema.Properties[TextProperty3].Type.Value.HasFlag(JsonSchemaType.Null));
    }

    public class Sample
    {
        public string PropertyWithNoRules { get; set; } = null!;
        public string NotNull { get; set; } = null!;
        public string NotEmpty { get; set; } = null!;
        public string EmailAddressRegex { get; set; } = null!;
        public string EmailAddress { get; set; } = null!;
        public string RegexField { get; set; } = null!;
        public int ValueInRange { get; set; }
        public int ValueInRangeExclusive { get; set; }
        public float ValueInRangeFloat { get; set; }
        public double ValueInRangeDouble { get; set; }
        public decimal DecimalValue { get; set; }
        public string NotEmptyWithMaxLength { get; set; } = null!;

        // ReSharper disable once InconsistentNaming = null!;
        // https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation/issues/10
        public string javaStyleProperty { get; set; } = null!;
    }

    public class SampleValidator : AbstractValidator<Sample>
    {
        public SampleValidator()
        {
            RuleFor(sample => sample.NotNull).NotNull();
            RuleFor(sample => sample.NotEmpty).NotEmpty();
            RuleFor(sample => sample.EmailAddressRegex).EmailAddress(EmailValidationMode.Net4xRegex);
            RuleFor(sample => sample.EmailAddress).EmailAddress(EmailValidationMode.AspNetCoreCompatible);
            RuleFor(sample => sample.RegexField).Matches(@"(\d{4})-(\d{2})-(\d{2})");

            RuleFor(sample => sample.ValueInRange).GreaterThanOrEqualTo(5).LessThanOrEqualTo(10);
            RuleFor(sample => sample.ValueInRangeExclusive).GreaterThan(5).LessThan(10);

            RuleFor(sample => sample.ValueInRangeFloat).InclusiveBetween(5.1f, 10.2f);
            RuleFor(sample => sample.ValueInRangeDouble).ExclusiveBetween(5.1, 10.2);
            RuleFor(sample => sample.DecimalValue).InclusiveBetween(1.333m, 200.333m);

            RuleFor(sample => sample.javaStyleProperty).MaximumLength(6);

            RuleFor(sample => sample.NotEmptyWithMaxLength).NotEmpty().MaximumLength(50);
        }
    }

    [Fact]
    public void SampleValidator_FromSampleApi_Test()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)SchemaGenerator(new SampleValidator())
            .GenerateSchema(typeof(Sample), schemaRepository);
        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];

        Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Object));
        Assert.Equal(13, schema.Properties.Keys.Count);


        Assert.False(schema.Properties["NotNull"].Type.Value.HasFlag(JsonSchemaType.Null));
        Assert.Contains("NotNull", schema.Required);

        Assert.Equal(1, schema.Properties["NotEmpty"].MinLength);

        Assert.NotNull(schema.Properties["EmailAddressRegex"].Pattern);
        Assert.NotEmpty(schema.Properties["EmailAddressRegex"].Pattern);
        Assert.Equal("email", schema.Properties["EmailAddressRegex"].Format);
        Assert.Equal("email", schema.Properties["EmailAddress"].Format);

        Assert.Equal(@"(\d{4})-(\d{2})-(\d{2})", schema.Properties["RegexField"].Pattern);

        Assert.Equal("5", schema.Properties["ValueInRange"].Minimum);
        Assert.Null(schema.Properties["ValueInRange"].ExclusiveMinimum);
        Assert.Equal("10", schema.Properties["ValueInRange"].Maximum);
        Assert.Null(schema.Properties["ValueInRange"].ExclusiveMaximum);

        Assert.Equal("5", schema.Properties["ValueInRangeExclusive"].ExclusiveMinimum);
        Assert.Equal("10", schema.Properties["ValueInRangeExclusive"].ExclusiveMaximum);

        Assert.Equal((decimal)5.1f,
            decimal.Parse(schema.Properties["ValueInRangeFloat"].Minimum, CultureInfo.InvariantCulture));
        Assert.Null(schema.Properties["ValueInRangeFloat"].ExclusiveMinimum);
        Assert.Equal((decimal)10.2f,
            decimal.Parse(schema.Properties["ValueInRangeFloat"].Maximum, CultureInfo.InvariantCulture));
        Assert.Null(schema.Properties["ValueInRangeFloat"].ExclusiveMaximum);

        Assert.Equal((decimal)5.1d,
            decimal.Parse(schema.Properties["ValueInRangeDouble"].ExclusiveMinimum, CultureInfo.InvariantCulture));
        Assert.Equal((decimal)10.2d,
            decimal.Parse(schema.Properties["ValueInRangeDouble"].ExclusiveMaximum, CultureInfo.InvariantCulture));

        Assert.Equal(1.333m, decimal.Parse(schema.Properties["DecimalValue"].Minimum, CultureInfo.InvariantCulture));
        Assert.Null(schema.Properties["DecimalValue"].ExclusiveMinimum);
        Assert.Equal(200.333m, decimal.Parse(schema.Properties["DecimalValue"].Maximum, CultureInfo.InvariantCulture));
        Assert.Null(schema.Properties["DecimalValue"].ExclusiveMaximum);

        Assert.Equal(1, schema.Properties["NotEmptyWithMaxLength"].MinLength);
        Assert.Equal(50, schema.Properties["NotEmptyWithMaxLength"].MaxLength);
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Surname { get; set; } = null!;
        public string Forename { get; set; } = null!;
        public decimal Discount { get; set; }
        public string Address { get; set; } = null!;
    }

    public class CustomerValidator : AbstractValidator<Customer>
    {
        public CustomerValidator()
        {
            When(customer => customer.Id == 1, () => {
                RuleFor(customer => customer.Discount)
                    .NotEmpty()
                    .WithMessage("This WILL NOT be in the OpenAPI  spec.");
            });

            RuleFor(customer => customer.Discount)
                .ExclusiveBetween(4, 5)
                .WithMessage("This WILL be in the OpenAPI spec.");
            RuleFor(customer => customer.Discount)
                .NotEmpty()
                .WhenAsync((customer, token) => Task.FromResult(customer.Id == 1))
                .WithMessage("This WILL NOT be in the OpenAPI spec.");

            RuleFor(customer => customer.Surname)
                .NotEmpty();

            RuleFor(customer => customer.Forename)
                .NotEmpty()
                .WithMessage("Please specify a first name");

            Include(new CustomerAddressValidator());
        }
    }

    internal class CustomerAddressValidator : AbstractValidator<Customer>
    {
        public CustomerAddressValidator()
        {
            UnlessAsync((customer, token) => Task.FromResult(customer.Surname == "Test"), () => {
                RuleFor(customer => customer.Discount)
                    .NotEmpty()
                    .WithMessage("This WILL NOT be in the OpenAPI spec.");
            });

            RuleFor(customer => customer.Discount)
                .NotEmpty()
                .Unless(customer => customer.Surname == "Test")
                .WithMessage("This WILL NOT be in the OpenAPI spec.");

            RuleFor(customer => customer.Address)
                .Length(20, 250);
        }
    }

    [Fact]
    public void CustomerValidator_FromSampleApi_Test()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)
            SchemaGenerator(new CustomerValidator()).GenerateSchema(typeof(Customer), schemaRepository);
        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];
        Assert.Equal(1, schema.Properties["Surname"].MinLength);
        Assert.Equal(1, schema.Properties["Forename"].MinLength);

        // From included validator
        Assert.Equal(20, schema.Properties["Address"].MinLength);
        Assert.Equal(250, schema.Properties["Address"].MaxLength);

        Assert.Equivalent(new OpenApiSchema() {
            Type = JsonSchemaType.Number,
            Format = "double",
            Minimum = null,
            ExclusiveMinimum = "4",
            Maximum = null,
            ExclusiveMaximum = "5",
        }, schema.Properties["Discount"]);

        Assert.Equal(5, schema.Properties.Keys.Count);
    }

    public class Person
    {
        public List<string> Emails { get; set; } = [];
    }

    public class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleForEach(x => x.Emails).EmailAddress();
        }
    }

    /// <summary>
    /// RuleForEach.
    /// https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation/issues/66
    /// </summary>
    [Fact]
    public void CollectionValidation()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)SchemaGenerator(new PersonValidator())
            .GenerateSchema(typeof(Person), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];
        var emailsProp = schema.Properties[nameof(Person.Emails)];

        Assert.Null(emailsProp.Format);

        Assert.True(emailsProp.Items.Type.Value.HasFlag(JsonSchemaType.String));
        Assert.Equal("email", emailsProp.Items.Format);
    }

    public class NumberEntity
    {
        public int Number { get; set; }
        public int? NullableNumber { get; set; }

        public class Validator : AbstractValidator<NumberEntity>
        {
            public Validator()
            {
                RuleFor(c => c.Number).GreaterThan(0);
                RuleFor(c => c.NullableNumber).GreaterThan(0);
            }
        }
    }

    /// <summary>
    /// https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation/issues/70
    /// </summary>
    [Fact]
    public void integer_property_should_not_be_nullable()
    {
        // *************************
        // FluentValidation behavior
        // *************************

        void ShouldBeSuccess(NumberEntity entity) => new NumberEntity.Validator().ValidateAndThrow(entity);
        void ShouldBeFailed(NumberEntity entity) => Assert.False(new NumberEntity.Validator().Validate(entity).IsValid);

        ShouldBeSuccess(new NumberEntity() { Number = 1 });
        ShouldBeFailed(new NumberEntity() { Number = 0 });

        ShouldBeSuccess(new NumberEntity() { Number = 1, NullableNumber = 1 });
        ShouldBeFailed(new NumberEntity() { Number = 1, NullableNumber = 0 });
        // null is also valid
        ShouldBeSuccess(new NumberEntity() { Number = 1, NullableNumber = null });

        // *********************************
        // FluentValidation swagger behavior
        // *********************************

        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)SchemaGenerator(new NumberEntity.Validator())
            .GenerateSchema(typeof(NumberEntity), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];

        var numberProp = schema.Properties[nameof(NumberEntity.Number)];
        Assert.True(numberProp.Type.Value.HasFlag(JsonSchemaType.Integer));
        Assert.False(numberProp.Type.Value.HasFlag(JsonSchemaType.Null));
        Assert.Equal("0", numberProp.ExclusiveMinimum);

        var nullableNumberProp = schema.Properties[nameof(NumberEntity.NullableNumber)];
        Assert.True(nullableNumberProp.Type.Value.HasFlag(JsonSchemaType.Integer));
        Assert.True(nullableNumberProp.Type.Value.HasFlag(JsonSchemaType.Null));
        Assert.Equal("0", nullableNumberProp.ExclusiveMinimum);
    }


    public class TestEntity
    {
        public string TextValue { get; set; }

        public string? NullableTextValue { get; set; }
    }

    public class TestNumberEntity
    {
        public int IntValue { get; set; }
        public int? NullableIntValue { get; set; }
    }

    [Fact]
    public void TextNullability()
    {
        new SchemaBuilder<TestEntity>()
            .AddRule(entity => entity.TextValue,
                rule => rule.MaximumLength(5),
                schema => Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null)));

        new SchemaBuilder<TestEntity>()
            .AddRule(entity => entity.NullableTextValue,
                rule => rule.MaximumLength(5),
                schema => Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null)));
    }

    [Fact]
    public void NotNull()
    {
        var property = new SchemaBuilder<TestEntity>()
            .AddRule(entity => entity.TextValue, rule => rule.NotNull().MinimumLength(1));

        Assert.False(property.Type.Value.HasFlag(JsonSchemaType.Null));
        Assert.Equal(1, property.MinLength);
    }

    [Fact]
    public void MinimumLength_ShouldNot_Set_Nullable_By_Default()
    {
        // without options. property is nullable, min length is set.
        new SchemaBuilder<TestEntity>()
            .AddRule(entity => entity.TextValue, rule => rule.MinimumLength(1), schema => {
                Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal(1, schema.MinLength);
            });

        new SchemaBuilder<TestEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinLengthGreaterThanZero = false)
            .AddRule(entity => entity.TextValue, rule => rule.MinimumLength(1), schema => {
                Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal(1, schema.MinLength);
            });

        new SchemaBuilder<TestEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinLengthGreaterThanZero = true)
            .AddRule(entity => entity.TextValue, rule => rule.MinimumLength(1), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal(1, schema.MinLength);
            });
    }

    [Fact]
    public void GreaterThan_ShouldRespect_Set_NotNullable_MinimumGreaterThanZero()
    {
        // without options. property is nullable, minimum is set.
        new SchemaBuilder<TestNumberEntity>()
            .AddRule(entity => entity.IntValue, rule => rule.GreaterThan(0), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.ExclusiveMinimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .AddRule(entity => entity.IntValue, rule => rule.GreaterThanOrEqualTo(1), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("1", schema.Minimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .AddRule(entity => entity.NullableIntValue, rule => rule.GreaterThan(0), schema => {
                Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.ExclusiveMinimum);
            });

        new SchemaBuilder<TestNumberEntity>()
            .AddRule(entity => entity.NullableIntValue, rule => rule.GreaterThanOrEqualTo(1), schema => {
                Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("1", schema.Minimum);
            });

        // SetNotNullableIfMinLengthGreaterThenZero = false
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = false)
            .AddRule(entity => entity.IntValue, rule => rule.GreaterThan(0), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.ExclusiveMinimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = false)
            .AddRule(entity => entity.IntValue, rule => rule.GreaterThanOrEqualTo(1), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("1", schema.Minimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = false)
            .AddRule(entity => entity.IntValue, rule => rule.GreaterThanOrEqualTo(0), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.Minimum);
            });
        // Nullable
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = false)
            .AddRule(entity => entity.NullableIntValue, rule => rule.GreaterThan(0), schema => {
                Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.ExclusiveMinimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = false)
            .AddRule(entity => entity.NullableIntValue, rule => rule.GreaterThanOrEqualTo(1), schema => {
                Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("1", schema.Minimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = false)
            .AddRule(entity => entity.NullableIntValue, rule => rule.GreaterThanOrEqualTo(0), schema => {
                Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.Minimum);
            });

        // SetNotNullableIfMinLengthGreaterThenZero = true
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = true)
            .AddRule(entity => entity.IntValue, rule => rule.GreaterThan(0), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.ExclusiveMinimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = true)
            .AddRule(entity => entity.IntValue, rule => rule.GreaterThanOrEqualTo(1), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("1", schema.Minimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = true)
            .AddRule(entity => entity.IntValue, rule => rule.GreaterThanOrEqualTo(0), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.Minimum);
            });
        //Nullable
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = true)
            .AddRule(entity => entity.NullableIntValue, rule => rule.GreaterThan(0), schema => {
                Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.ExclusiveMinimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = true)
            .AddRule(entity => entity.NullableIntValue, rule => rule.GreaterThanOrEqualTo(1), schema => {
                Assert.False(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("1", schema.Minimum);
            });
        new SchemaBuilder<TestNumberEntity>()
            .ConfigureSchemaGenerationOptions(options => options.SetNotNullableIfMinimumGreaterThanZero = true)
            .AddRule(entity => entity.NullableIntValue, rule => rule.GreaterThanOrEqualTo(0), schema => {
                Assert.True(schema.Type.Value.HasFlag(JsonSchemaType.Null));
                Assert.Equal("0", schema.Minimum);
            });
    }

    public class BestShot
    {
        [JsonPropertyName("photo")]
        public string Link { get; set; }

        [JsonPropertyName("zone")]
        public string Area { get; set; }
    }

    [Fact]
    public void NameOverrides()
    {
        new SchemaBuilder<BestShot>()
            .AddRule(entity => entity.Link,
                rule => rule.MinimumLength(5),
                schema => Assert.Equal(5, schema.MinLength));
    }

    public class MinMaxLength
    {
        public string Name { get; set; }
        public List<string> Qualities { get; set; }
    }

    public class MinMaxLengthValidator : AbstractValidator<MinMaxLength>
    {
        public MinMaxLengthValidator(int min, int max)
        {
            RuleFor(x => x.Name).MinimumLength(min).MaximumLength(max);
            RuleFor(x => x.Qualities).ListRange(min, max);
        }
    }

    [Theory]
    [InlineData(0, 40)]
    [InlineData(10, 0)]
    [InlineData(3, 40)]
    public void ILengthValidator_ProperlyAppliesMinMax_ToStrings(int min, int max)
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)SchemaGenerator(new MinMaxLengthValidator(min, max))
            .GenerateSchema(typeof(MinMaxLength), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];

        if (min > 0)
            Assert.Equal(min, schema.Properties[nameof(MinMaxLength.Name)].MinLength);
        else
            Assert.Null(schema.Properties[nameof(MinMaxLength.Name)].MinLength);
        if (max > 0)
            Assert.Equal(max, schema.Properties[nameof(MinMaxLength.Name)].MaxLength);
        else
            Assert.Null(schema.Properties[nameof(MinMaxLength.Name)].MaxLength);

        // MinItems / MaxItems shoiuld not be set for strings
        Assert.Null(schema.Properties[nameof(MinMaxLength.Name)].MinItems);
        Assert.Null(schema.Properties[nameof(MinMaxLength.Name)].MaxItems);
    }

    [Theory]
    [InlineData(0, 40)]
    [InlineData(10, 0)]
    [InlineData(3, 40)]
    public void ILengthValidator_ProperlyAppliesMinMax_ToArrays(int min, int max)
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)SchemaGenerator(new MinMaxLengthValidator(min, max))
            .GenerateSchema(typeof(MinMaxLength), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];

        // MinLength / MaxLength should not be set for arrays
        Assert.Null(schema.Properties[nameof(MinMaxLength.Qualities)].MinLength);
        Assert.Null(schema.Properties[nameof(MinMaxLength.Qualities)].MaxLength);

        if (min > 0)
            Assert.Equal(min, schema.Properties[nameof(MinMaxLength.Qualities)].MinItems);
        else
            Assert.Null(schema.Properties[nameof(MinMaxLength.Qualities)].MinItems);
        if (max > 0)
            Assert.Equal(max, schema.Properties[nameof(MinMaxLength.Qualities)].MaxItems);
        else
            Assert.Null(schema.Properties[nameof(MinMaxLength.Qualities)].MaxItems);
    }

    [Fact]
    // See the issue https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation/issues/156
    public void DerivedSample_ShouldHave_ValidationRulesApplied()
    {
        var schemaRepository = new SchemaRepository();
        var schemaGenerator = SchemaGenerator(options => {
            options.UseAllOfForInheritance = true;
            ConfigureGenerator(options, [new DervidedSampleValidator()]);
        });

        schemaGenerator.GenerateSchema(typeof(BaseSample), schemaRepository);
        var referenceSchema =
            (OpenApiSchemaReference)schemaGenerator.GenerateSchema(typeof(DerivedSample), schemaRepository);
        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];

        // Should be empty after the change made because of the issue
        // https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/3021
        Assert.True(schema.Properties == null || !schema.Properties.Any());
        Assert.Equal(2, schema.AllOf.Count);

        var derivedSampleSchema = schema.AllOf.FirstOrDefault(s => s.Type == JsonSchemaType.Object);

        Assert.NotNull(derivedSampleSchema);

        Assert.Single(derivedSampleSchema.Properties);
        var propertySchema = derivedSampleSchema.Properties.First().Value;
        Assert.Equal(255, propertySchema.MaxLength);
        Assert.Equal(1, propertySchema.MinLength);

        Assert.Single(derivedSampleSchema.Required);
        Assert.Equal("Name", derivedSampleSchema.Required.First());
    }
}

public class BaseSample
{
    public int Id { get; set; }
}

public class DerivedSample : BaseSample
{
    public string? Name { get; set; }
}

public class DervidedSampleValidator : AbstractValidator<DerivedSample>
{
    public DervidedSampleValidator()
    {
        RuleFor(p => p.Name).NotEmpty().MaximumLength(255);
    }
}

public static class ValidatorExtensions
{
    public static IRuleBuilderOptions<T, TProperty> ListRange<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder,
        int minimumLenghtInclusive,
        int maximumLengthInclusive
    )
        where TProperty : IEnumerable
    {
        return ruleBuilder.SetValidator(
            (IPropertyValidator<T, TProperty>)new ListLengthValidator<T, TProperty>(minimumLenghtInclusive,
                maximumLengthInclusive));
    }

    private sealed class ListLengthValidator<T, TProperty> : PropertyValidator<T, TProperty>, ILengthValidator
    {
        public ListLengthValidator(int minimumLength, int maximumLength)
        {
            Min = minimumLength;
            Max = maximumLength;
        }

        public int Min { get; }

        public int Max { get; }

        public override string Name => nameof(ListLengthValidator<T, TProperty>);

        public override bool IsValid(ValidationContext<T> context, TProperty value)
        {
            if (value is IList listvalue) {
                return listvalue.Count >= this.Min && listvalue.Count <= this.Max;
            }

            return true;
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            if (this.Min == 0) {
                return $"The number of elements in '{{PropertyName}}' must not exceed '{Max}'.";
            }
            else {
                return $"The number of elements in '{{PropertyName}}' must be between '{Min}' and '{Max}'.";
            }
        }
    }
}