using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebBackend.Swager
{
    public class RefreshTokenOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Refresh-Token",
                In = ParameterLocation.Header,
                Description = "Refresh Token",
                Required = false,
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }
}
