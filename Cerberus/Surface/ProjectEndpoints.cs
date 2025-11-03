using System;

using Cerberus.Application;
using Cerberus.Domain;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cerberus.Surface;

public static class ProjectEndpoints
{
    public static WebApplication MapProjectEndpoints(this WebApplication application)
    {
        var group = application.MapGroup("/cerberus/tenants/{tenantId:guid}/projects")
            .WithTags("Projects")
            .WithDescription("Manage projects within tenants");

        MapProjectManagement(group);

        return application;
    }

    private static void MapProjectManagement(RouteGroupBuilder group)
    {
        // GET specific project by tenant ID and project ID
        group.MapGet("/{projectId:guid}", async (
            Guid tenantId,
            Guid projectId,
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if (apiKey is null)
            {
                return Results.Unauthorized();
            }

            var tenant = await tenantService.GetTenantByIdAsync(tenantId);
            var project = tenant?.Projects.FirstOrDefault(p => p.Id == projectId);

            // Return 404 if tenant/project doesn't exist OR if API key doesn't have access
            // This prevents information disclosure about resource existence
            if (tenant is null || project is null || !apiKeyService.HasProjectAccess(apiKey, tenantId, projectId))
            {
                return Results.NotFound(new { message = $"Project with ID {projectId} not found in tenant {tenantId}" });
            }

            return Results.Ok(project);
        })
        .WithName("GetProjectById")
        .WithSummary("Get project by ID")
        .WithDescription("Retrieves details for a specific project within a tenant.");

        // GET all animas (secrets) for a specific project
        group.MapGet("/{projectId:guid}/animas", async (
            Guid tenantId,
            Guid projectId,
            HttpContext httpContext,
            [FromQuery] string? environment,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if (apiKey is null)
            {
                return Results.Unauthorized();
            }

            var tenant = await tenantService.GetTenantByIdAsync(tenantId);
            var project = tenant?.Projects.FirstOrDefault(p => p.Id == projectId);

            // Return 404 if tenant/project doesn't exist OR if API key doesn't have access
            // This prevents information disclosure about resource existence
            if (tenant is null || project is null || !apiKeyService.HasProjectAccess(apiKey, tenantId, projectId))
            {
                return Results.NotFound(new { message = $"Project with ID {projectId} not found in tenant {tenantId}" });
            }
            if(environment is not null &&!Enum.GetValues<EnvironmentType>().Select(x => x.ToString().ToLower()).Any(e => e.Equals(environment.ToLower())))
            {
                return Results.BadRequest($"The requested environment type: {environment} does not exist");
            }
            var animas = environment is null 
            ? project.Animas 
            : project.Animas.Where(x=>x.Environment.ToString().Equals( environment,StringComparison.InvariantCultureIgnoreCase));

            return Results.Ok(animas);
        })
        .WithName("GetProjectAnimas")
        .WithSummary("List secrets for a project")
        .WithDescription("Retrieves all secrets (animas) stored in a specific project.");

        // POST create a new project
        group.MapPost("", async (
            Guid tenantId,
            [FromBody] CreateProjectRequest request,
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if (apiKey is null)
            {
                return Results.Unauthorized();
            }

            // Check if API key has access to this tenant
            if (!apiKeyService.HasTenantAccess(apiKey, tenantId))
            {
                return Results.Json(new { message = "Access denied to this tenant" }, statusCode: 403);
            }

        var projectId = await tenantService.CreateProjectAsync(
                tenantId,
                request.Name,
                request.Description);

            return Results.Created($"/tenants/{tenantId}/projects/{projectId}", new
            {
                id = projectId,
                tenantId,
                name = request.Name,
                description = request.Description
            });
        })
        .WithName("CreateProject")
        .WithSummary("Create a new project")
        .WithDescription("Creates a new project within a tenant. Must specify environment (DEVELOPMENT, STAGING, or PRODUCTION).");
    }
}

public record CreateProjectRequest(string Name, string Description, string Environment);
