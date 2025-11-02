using Cerberus.Domain;
using Cerberus.Infrastructure;

namespace Cerberus.Application;

public class TenantService
{
    private readonly TenantRepository _tenantRepository;

    public TenantService(TenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
    {
        return await _tenantRepository.GetAllAsync();
    }

    public async Task<Tenant?> GetTenantByIdAsync(Guid id)
    {
        return await _tenantRepository.GetByIdAsync(id);
    }

    public async Task<Guid> CreateTenantAsync(string name)
    {
        return await _tenantRepository.CreateTenantAsync(name);
    }

    public async Task<Guid> CreateProjectAsync(Guid tenantId, string name, string description)
    {
        return await _tenantRepository.CreateProjectAsync(tenantId, name, description);
    }

    public async Task<Guid> CreateAnimaAsync(Guid projectId, string definition, string value, string description,EnvironmentType environment)
    {
        return await _tenantRepository.CreateAnimaAsync(projectId, definition, value,  description, environment);
    }

    public async Task<bool> DeleteAnimaAsync(string definition)
    {
        return await _tenantRepository.DeleteAnimaAsync(definition);
    }

    public async Task<bool> UpdateAnimaAsync(string definition, string value,EnvironmentType environment, string? description = null)
    {
        return await _tenantRepository.UpdateAnimaAsync(definition, value, environment, description);
    }
}
