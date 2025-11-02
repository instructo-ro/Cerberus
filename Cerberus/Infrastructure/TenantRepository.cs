using Cerberus.Domain;
using Dapper;

namespace Cerberus.Infrastructure;

public class TenantRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TenantRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                t.id, t.name,
                p.id, p.name, p.description, p.environment,
                a.definition, a.value, a.description
            FROM tenants t
            LEFT JOIN projects p ON p.tenant_id = t.id
            LEFT JOIN animas a ON a.project_id = p.id
            WHERE t.id = @Id";

        var tenantDict = new Dictionary<Guid, Tenant>();
        var projectDict = new Dictionary<Guid, Project>();

        await connection.QueryAsync<TenantDto, ProjectDto?, AnimaDto?, Tenant>(
            sql,
            (tenantDto, projectDto, animaDto) =>
            {
                if (!tenantDict.TryGetValue(tenantDto.Id, out var tenant))
                {
                    var projectsList = new List<Project>();
                    tenant = new Tenant(
                        Id: tenantDto.Id,
                        Name: tenantDto.Name,
                        Projects: projectsList
                    );
                    tenantDict.Add(tenant.Id, tenant);
                }

                if (projectDto != null && !projectDict.ContainsKey(projectDto.Id))
                {
                    var animasList = new List<Anima>();
                    var project = new Project(
                        Id: projectDto.Id,
                        Name: projectDto.Name,
                        Description: projectDto.Description,
                        Environment: Enum.Parse<Domain.EnvironmentType>(projectDto.Environment),
                        Animas: animasList
                    );
                    projectDict.Add(project.Id, project);
                    ((List<Project>)tenant.Projects).Add(project);
                }

                if (animaDto != null && projectDto != null)
                {
                    var project = projectDict[projectDto.Id];
                    var anima = new Anima(
                        Definition: animaDto.Definition,
                        Value: animaDto.Value,
                        Description: animaDto.Description
                    );
                    ((List<Anima>)project.Animas).Add(anima);
                }

                return tenant;
            },
            new { Id = id },
            splitOn: "id,definition"
        );

        return tenantDict.Values.FirstOrDefault();
    }

    public async Task<IEnumerable<Tenant>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                t.id, t.name,
                p.id, p.name, p.description, p.environment,
                a.definition, a.value, a.description
            FROM tenants t
            LEFT JOIN projects p ON p.tenant_id = t.id
            LEFT JOIN animas a ON a.project_id = p.id
            ORDER BY t.name";

        var tenantDict = new Dictionary<Guid, Tenant>();
        var projectDict = new Dictionary<Guid, Project>();

        await connection.QueryAsync<TenantDto, ProjectDto?, AnimaDto?, Tenant>(
            sql,
            (tenantDto, projectDto, animaDto) =>
            {
                if (!tenantDict.TryGetValue(tenantDto.Id, out var tenant))
                {
                    var projectsList = new List<Project>();
                    tenant = new Tenant(
                        Id: tenantDto.Id,
                        Name: tenantDto.Name,
                        Projects: projectsList
                    );
                    tenantDict.Add(tenant.Id, tenant);
                }

                if (projectDto != null && !projectDict.ContainsKey(projectDto.Id))
                {
                    var animasList = new List<Anima>();
                    var project = new Project(
                        Id: projectDto.Id,
                        Name: projectDto.Name,
                        Description: projectDto.Description,
                        Environment: Enum.Parse<Domain.EnvironmentType>(projectDto.Environment),
                        Animas: animasList
                    );
                    projectDict.Add(project.Id, project);
                    ((List<Project>)tenant.Projects).Add(project);
                }

                if (animaDto != null && projectDto != null && projectDict.ContainsKey(projectDto.Id))
                {
                    var project = projectDict[projectDto.Id];
                    var anima = new Anima(
                        Definition: animaDto.Definition,
                        Value: animaDto.Value,
                        Description: animaDto.Description
                    );
                    ((List<Anima>)project.Animas).Add(anima);
                }

                return tenant;
            },
            splitOn: "id,definition"
        );

        return tenantDict.Values;
    }

    public async Task<Guid> CreateTenantAsync(string name)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO tenants (name)
            VALUES (@Name)
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, new { Name = name });
    }

    public async Task<Guid> CreateProjectAsync(Guid tenantId, string name, string description, string environment)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO projects (tenant_id, name, description,environment)
            VALUES (@TenantId, @Name, @Description,@Environment)
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            TenantId = tenantId,
            Name = name,
            Description = description,
            Environment = environment
        });
    }

    public async Task<Guid> CreateAnimaAsync(Guid projectId, string definition, string value, string description)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO animas (project_id, definition, value, description)
            VALUES (@ProjectId, @Definition, @Value, @Description)
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            ProjectId = projectId,
            Definition = definition,
            Value = value,
            Description = description
        });
    }

    public async Task<bool> DeleteAnimaAsync(string definition)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = "DELETE FROM animas WHERE Definition = @definition";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Definition = definition });
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateAnimaAsync(string definition, string value, string? description = null)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            UPDATE animas
            SET value = @Value,
                description = COALESCE(@Description, description)
            WHERE Definition = @Definition";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Definition = definition,
            Value = value,
            Description = description
        });

        return rowsAffected > 0;
    }
     
    // DTOs for mapping
    private record TenantDto(Guid Id, string Name);
    private record ProjectDto(Guid Id, string Name, string Description, string Environment);
    private record AnimaDto(string Definition, string Value, string Description);
}
