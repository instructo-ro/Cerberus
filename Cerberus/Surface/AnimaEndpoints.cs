using Cerberus.Application;
using Cerberus.Domain;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cerberus.Surface;

public static class AnimaEndpoints
{
    public static WebApplication MapAnimaEndpoints(this WebApplication application)
    {
        var group = application.MapGroup("/cerberus/tenants/{tenantId:guid}/projects/{projectId:guid}/animas")
            .WithTags("Secrets (Animas)")
            .WithDescription("Manage secrets (animas) within projects, for UI client");

        MapAnimaManagement(group);
        application.MapSpecialRetrievalEndpoint();

        return application;
    }

    private static void MapSpecialRetrievalEndpoint(this WebApplication webApplication)
    {
        webApplication.MapGet("/animas/{definition}",async (
            string definition,
            [FromQuery] string? environment,
            [FromQuery] Guid? projectId,
            HttpContext httpContext,
            [FromServices] ApiKeyService keyService,
            [FromServices] TenantService tenantService
            ) =>
        {
            var apiKey = httpContext.GetApiKey();
            if(apiKey is null)
            {
                return Results.Unauthorized();
            }
            var tenant = await tenantService.GetTenantByIdAsync(apiKey.TenantId);
            var project = projectId is null ? tenant?.Projects.FirstOrDefault() : tenant?.Projects.FirstOrDefault(x=>x.Id == projectId);
            var anima = project?.Animas.FirstOrDefault(a => a.Definition.Equals(definition, StringComparison.OrdinalIgnoreCase) && a.Environment.ToString() == (environment??EnvironmentType.DEVELOPMENT.ToString()));
            if(anima is null)
            {
                return Results.Unauthorized();
            }
            return Results.Ok(anima);
        }).WithName("Anima retrieval");
    }

    private static void MapAnimaManagement(RouteGroupBuilder group)
    {
        // GET specific anima (secret) by definition name
        group.MapGet("/{definition}", async (
            Guid tenantId,
            Guid projectId,
            string definition,
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if(apiKey is null)
            {
                return Results.Unauthorized();
            }

            var tenant = await tenantService.GetTenantByIdAsync(tenantId);
            var project = tenant?.Projects.FirstOrDefault(p => p.Id==projectId);
            var anima = project?.Animas.FirstOrDefault(a => a.Definition.Equals(definition, StringComparison.OrdinalIgnoreCase));

            // Return 404 if resources don't exist OR if API key doesn't have access
            // This prevents information disclosure about resource existence
            if(tenant is null || project is null || anima is null || !apiKeyService.HasProjectAccess(apiKey, tenantId, projectId))
            {
                return Results.NotFound(new { message = $"Anima with definition '{definition}' not found in project {projectId}" });
            }

            return Results.Ok(anima);
        })
        .WithName("GetAnimaByDefinition")
        .WithSummary("Get secret by name")
        .WithDescription("Retrieves a specific secret by its definition name (e.g., 'DATABASE_URL').");



        // POST create a new anima (secret)
        group.MapPost("", async (
            Guid tenantId,
            Guid projectId,
            [FromBody] CreateAnimaRequest request,
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if(apiKey is null)
            {
                return Results.Unauthorized();
            }

            // Verify project exists and API key has access
            var tenant = await tenantService.GetTenantByIdAsync(tenantId);
            var project = tenant?.Projects.FirstOrDefault(p => p.Id==projectId);

            // Return 404 if project doesn't exist OR if API key doesn't have access
            // This prevents information disclosure about resource existence
            if(tenant is null || project is null || !apiKeyService.HasProjectAccess(apiKey, tenantId, projectId))
            {
                return Results.NotFound(new { message = $"Project with ID {projectId} not found in tenant {tenantId}" });
            }

            var animaId = await tenantService.CreateAnimaAsync(
                projectId,
                request.Definition,
                request.Value,
                request.Description,
                Enum.Parse<Domain.EnvironmentType>(request.Environment));

            return Results.Created($"/tenants/{tenantId}/projects/{projectId}/animas/{request.Definition}", new
            {
                id = animaId,
                definition = request.Definition,
                description = request.Description
            });
        })
        .WithName("CreateAnima")
        .WithSummary("Create a new secret")
        .WithDescription("Creates a new secret (anima) in the project with a definition name, value, and optional description.");

        // PUT update an existing anima's value
        group.MapPut("/{definition}", async (
            Guid tenantId,
            Guid projectId,
            string definition,
            [FromBody] UpdateAnimaRequest request,
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if(apiKey is null)
            {
                return Results.Unauthorized();
            }

            // Verify project exists and API key has access before attempting update
            var tenant = await tenantService.GetTenantByIdAsync(tenantId);
            var project = tenant?.Projects.FirstOrDefault(p => p.Id==projectId);

            // Return 404 if project doesn't exist OR if API key doesn't have access
            // This prevents information disclosure about resource existence
            if(tenant is null || project is null || !apiKeyService.HasProjectAccess(apiKey, tenantId, projectId))
            {
                return Results.NotFound(new { message = $"Anima with ID {definition} not found" });
            }

            var success = await tenantService.UpdateAnimaAsync(definition, request.Value, Enum.Parse<Domain.EnvironmentType>(request.Environment), request.Description);

            if(!success)
            {
                return Results.NotFound(new { message = $"Anima with ID {definition} not found" });
            }

            return Results.Ok(new { message = "Anima updated successfully" });
        })
        .WithName("UpdateAnima")
        .WithSummary("Update a secret")
        .WithDescription("Updates the value and/or description of an existing secret.");

        // DELETE remove an anima
        group.MapDelete("/{definition}", async (
            Guid tenantId,
            Guid projectId,
            string definition,
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if(apiKey is null)
            {
                return Results.Unauthorized();
            }

            // Verify project exists and API key has access before attempting delete
            var tenant = await tenantService.GetTenantByIdAsync(tenantId);
            var project = tenant?.Projects.FirstOrDefault(p => p.Id==projectId);

            // Return 404 if project doesn't exist OR if API key doesn't have access
            // This prevents information disclosure about resource existence
            if(tenant is null || project is null || !apiKeyService.HasProjectAccess(apiKey, tenantId, projectId))
            {
                return Results.NotFound(new { message = $"Anima with ID {definition} not found" });
            }

            var success = await tenantService.DeleteAnimaAsync(definition);

            if(!success)
            {
                return Results.NotFound(new { message = $"Anima with ID {definition} not found" });
            }

            return Results.Ok(new { message = "Anima deleted successfully" });
        })
        .WithName("DeleteAnima")
        .WithSummary("Delete a secret")
        .WithDescription("Permanently deletes a secret from the project.");
    }


}

public record CreateAnimaRequest(string Definition, string Value, string Description,string Environment);
public record UpdateAnimaRequest(string Value,string Environment, string? Description);
