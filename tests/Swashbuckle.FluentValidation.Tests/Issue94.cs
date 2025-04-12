using FluentValidation;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.FluentValidation.Tests;

/// <summary>
/// https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation/issues/94
/// </summary>
public class Issue94 : UnitTestBase
{
    public class Person
    {
        public string Name { get; set; }

        public List<string> Emails { get; set; } = new List<string>();

        private string FirstName
        {
            get
            {
                return this.Name == null ? null :
                    !this.Name.Contains(" ") ? this.Name :
                    this.Name.Split(new char[] { ' ' }).FirstOrDefault();
            }
        }

        private string LastName
        {
            get
            {
                return this.Name == null ? null :
                    !this.Name.Contains(" ") ? null :
                    this.Name.Split(new char[] { ' ' }).Skip(1).FirstOrDefault();
            }
        }

        //[UsedImplicitly]
        public class PersonValidator : AbstractValidator<Person>
        {
            public PersonValidator()
            {
                this.RuleFor(x => x.FirstName)
                    .MaximumLength(50)
                    .OverridePropertyName(x => x.Name)
                    .WithName("First Name")
                    ;

                this.RuleFor(x => x.LastName)
                    .MaximumLength(50)
                    .OverridePropertyName(x => x.Name)
                    .WithName("Last Name")
                    ;

                this.RuleFor(x => x.Name)
                    .MaximumLength(101);

                RuleForEach(x => x.Emails).EmailAddress();
            }
        }
    }

    [Fact]
    public void all_rules_should_be_applied()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = SchemaGenerator(new Person.PersonValidator())
            .GenerateSchema(typeof(Person), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];
        Assert.Equivalent(new string[] { "Name", "Emails" }, schema.Properties.Keys);
        var nameProperty = schema.Properties[nameof(Person.Name)];
        Assert.Equal("string", nameProperty.Type);
        Assert.Equal(101, nameProperty.MaxLength);
    }
}