using FluentValidation;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.FluentValidation.Tests;

public class SetValidatorShouldNotSetParentPropertiesTest : UnitTestBase
{
    public class Model
    {
        public IList<string> ListItems { get; set; }
        public SubModel SubModel { get; set; }
    }

    public class SubModel
    {
        public IList<string> ListItems { get; set; }
    }

    public class ModelValidator : AbstractValidator<Model>
    {
        public ModelValidator()
        {
            RuleFor(e => e.SubModel).SetValidator(new SubModelValidator());
        }
    }
    public class SubModelValidator : AbstractValidator<SubModel>
    {
        public SubModelValidator()
        {
            RuleFor(e => e.ListItems).NotEmpty();
        }
    }

    /// <summary>
    /// https://github.com/micro-elements/MicroElements.Swashbuckle.FluentValidation/issues/76
    /// </summary>
    [Fact]
    public void SetValidatorShouldNotSetParentProperties()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = SchemaGenerator(new ModelValidator()).GenerateSchema(typeof(Model), schemaRepository);

        var modelSchema = schemaRepository.Schemas["Model"];
        var listItemsProperty = modelSchema.Properties[nameof(Model.ListItems)];
        var subModelProperty = modelSchema.Properties[nameof(Model.SubModel)];
        Assert.NotNull(listItemsProperty);
        Assert.NotNull(subModelProperty);
        // because: "No required in Model"
        Assert.Empty(modelSchema.Required);

        var subModelSchema = schemaRepository.Schemas["SubModel"];
        var subModelListItemsProperty = subModelSchema.Properties[nameof(SubModel.ListItems)];
        Assert.NotNull(subModelListItemsProperty);
        // because: "ListItems is required in SubModel"
        Assert.Contains(nameof(SubModel.ListItems), subModelSchema.Required);
        Assert.Single(subModelSchema.Required);
    }
}