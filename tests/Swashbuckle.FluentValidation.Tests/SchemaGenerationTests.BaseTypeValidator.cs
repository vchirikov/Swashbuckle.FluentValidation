using FluentValidation;
using OpenApi.FluentValidation;

namespace Swashbuckle.FluentValidation.Tests;

public partial class SchemaGenerationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BaseTypeValidator(bool searchBaseTypeValidators)
    {
        new SwaggerTestHost()
            .Configure(options => options.ValidatorSearch = new ValidatorSearchSettings { SearchBaseTypeValidators = searchBaseTypeValidators })
            .RegisterValidator<AbstractInstitutionModelValidator>()
            .GenerateSchema<AbstractInstitutionModel>(out var schemaBase)
            .GenerateSchema<InstitutionModel>(out var schemaChild);

        if (searchBaseTypeValidators) {
            Assert.Equal(100, schemaBase.Properties["Name"].MaxLength);
            Assert.Equal(100, schemaChild.Properties["Name"].MaxLength);
        }
        else {
            Assert.Equal(100, schemaBase.Properties["Name"].MaxLength);
            // because: "Validator is for base type and no validator was for concrete type"
            Assert.Null(schemaChild.Properties["Name"].MaxLength);
        }
    }

    // abstract
    public abstract class AbstractInstitutionModel
    {
        public string Name { get; set; }
    }

    // class
    public class InstitutionModel : AbstractInstitutionModel
    {
    }

    // fluent validator
    public class AbstractInstitutionModelValidator : AbstractValidator<AbstractInstitutionModel>
    {
        public AbstractInstitutionModelValidator()
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .MaximumLength(100);
        }
    }
}