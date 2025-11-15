using FluentValidation;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Swashbuckle.FluentValidation.Tests;

public class TreeValidatorTest : UnitTestBase
{
    /// <summary>
    /// A simple recursive data structure
    /// </summary>
    public class Node
    {
        public string Id { get; set; }
        public List<Node> Nodes = new();
    }

    public class NodeValidator : AbstractValidator<Node>
    {
        public NodeValidator()
        {
            // Recursive validation example I found on this closed "issue".  Used it as the base for this test.
            // https://github.com/FluentValidation/FluentValidation/issues/1568
            RuleForEach(r => r.Nodes).SetValidator(this);
            RuleFor(r => r.Id).NotEmpty();
        }
    }

    [Fact]
    public void recursive_validator_works()
    {
        // *********************************
        // FluentValidation swagger behavior
        // *********************************

        var schemaRepository = new SchemaRepository();

        var referenceSchema = SchemaGenerator(new NodeValidator())
            .GenerateSchema(typeof(Node), schemaRepository);

        // if we have gotten this far, huzza, no stack overflow.

        // schema validation should work normally on other non-recursive properties
        if (!(referenceSchema is OpenApiSchemaReference { Reference.Id: not null } reference &&
              schemaRepository.Schemas.TryGetValue(reference.Reference.Id, out var schema))) {
            schema = null;
        }

        var idProperty = schema.Properties[nameof(Node.Id)];
        Assert.True(idProperty.Type.Value.HasFlag(JsonSchemaType.String));
        Assert.Equal(1, idProperty.MinLength);
    }
}