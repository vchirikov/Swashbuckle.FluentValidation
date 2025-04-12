using FluentValidation;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.FluentValidation.Tests;

/// <summary>
/// https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation/issues/92
/// </summary>
public class Issue92 : UnitTestBase
{
    // Command object to send in a POST call.
    public class CreateBookshelfCommand
    {
        public string[] Books { get; set; }
    }

    // Validator to validate that command.
    public class CreateBookshelfCommandValidator : AbstractValidator<CreateBookshelfCommand>
    {
        public CreateBookshelfCommandValidator()
        {
            RuleFor(c => c.Books)
                .NotEmpty();

            RuleForEach(c => c.Books)
                .NotEmpty()
                .MinimumLength(5)
                .MaximumLength(250);
        }
    }

    [Fact]
    public void all_rules_should_be_applied()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = SchemaGenerator(new CreateBookshelfCommandValidator()).GenerateSchema(typeof(CreateBookshelfCommand), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];
        var booksProperty = schema.Properties[nameof(CreateBookshelfCommand.Books)];
        Assert.Equal("array", booksProperty.Type);

        // should use MinItems for array
        Assert.Equal(1, booksProperty.MinItems);
        Assert.Null(booksProperty.MinLength);

        // items validation should be set
        Assert.Equal("string", booksProperty.Items.Type);
        Assert.Equal(5, booksProperty.Items.MinLength);
        Assert.Equal(250, booksProperty.Items.MaxLength);
    }
}