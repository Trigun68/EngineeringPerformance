using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EngineeringPerformance.Api.Swagger;

/// <summary>Описывает multipart/form-data для действий с IFormFile.</summary>
public sealed class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formFileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile)
                        || p.ModelMetadata?.ModelType == typeof(IFormFileCollection))
            .ToList();

        if (formFileParams.Count == 0)
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = formFileParams.ToDictionary(
                            p => p.Name ?? "file",
                            _ => new OpenApiSchema { Type = "string", Format = "binary" },
                            StringComparer.OrdinalIgnoreCase),
                        Required = formFileParams
                            .Select(p => p.Name ?? "file")
                            .ToHashSet(StringComparer.OrdinalIgnoreCase),
                    },
                },
            },
        };

        // Remove broken form parameters that Swashbuckle cannot serialize
        operation.Parameters = operation.Parameters?
            .Where(p => formFileParams.All(f =>
                !string.Equals(f.Name, p.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
