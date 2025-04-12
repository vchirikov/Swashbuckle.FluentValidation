using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        var referenceSchema = SchemaGenerator(new UploadFileRequestValidator()).GenerateSchema(typeof(UploadFileRequest), schemaRepository);

        var schema = schemaRepository.Schemas[referenceSchema.Reference.Id];
        var fileProperty = schema.Properties[nameof(UploadFileRequest.File)];
        Assert.Equal("string", fileProperty.Type);
        Assert.Equal("binary", fileProperty.Format);
        Assert.False(fileProperty.Nullable);
    }
}
