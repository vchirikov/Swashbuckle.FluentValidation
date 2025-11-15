using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.FluentValidation.Tests;

public class IFormFileTests : UnitTestBase
{
    public class UploadFileRequest
    {
        [FromForm(Name = "File")]
        public IFormFile File { get; set; }
    }

    public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
    {
        public UploadFileRequestValidator()
        {
            RuleFor(c => c.File)
                .Cascade(CascadeMode.Stop)
                .NotEmpty();
        }
    }

    [Fact]
    public void all_rules_should_be_applied()
    {
        var schemaRepository = new SchemaRepository();
        var referenceSchema = (OpenApiSchemaReference)SchemaGenerator(new UploadFileRequestValidator())
            .GenerateSchema(typeof(UploadFileRequest), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];
        var fileProperty = schema.Properties[nameof(UploadFileRequest.File)];
        Assert.True(fileProperty.Type.Value.HasFlag(JsonSchemaType.String));
        Assert.Equal("binary", fileProperty.Format);
        Assert.False(fileProperty.Type.Value.HasFlag(JsonSchemaType.Null));
    }
}