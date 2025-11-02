# Cerberus - Secrets Management System

A secure API-based secrets management system using PostgreSQL + Dapper with API key authentication.

Showcase of the API can be found [here](https://instructo-ro.github.io/Cerberus/)

## Quick Start

### Run everything with Docker:
```bash
# Start both database and API
docker-compose up -d

# View logs
docker-compose logs -f

# Stop everything
docker-compose down

# Reset database (WARNING: deletes all data)
docker-compose down -v
docker-compose up -d
```

### Run API locally (for development):
```bash
# Start only the database
docker-compose up -d postgres

# Run the API from Visual Studio or:
dotnet run --project Cerberus/Cerberus.csproj
```

## Endpoints

- **API (Docker)**: http://localhost:5000
- **API (Local dev)**: https://localhost:32773 (or check launchSettings.json)
- **Database**: localhost:5433

## API Overview

### Authentication
All endpoints (except API key creation) require an API key in the `Authorization` header:
```
Authorization: Bearer cerb_xxxxxxxxxxxxx
```

## Quick Start Workflow

### STEP 0: Bootstrap (First Time Setup ONLY)

**Solves the chicken-and-egg problem**: You need an API key to create a tenant, but you need a tenant to create an API key!

**Solution**: Use the `/bootstrap` endpoint ONCE to create your first tenant + master API key:

```http
POST /bootstrap
Content-Type: application/json

{
  "bootstrapToken": "CHANGE_THIS_IN_PRODUCTION_VIA_ENV_VAR",
  "tenantName": "My Organization",
  "apiKeyName": "Master API Key",
  "expiresAt": null
}
```

Response:
```json
{
  "tenantId": "guid",
  "tenantName": "My Organization",
  "apiKeyId": "guid",
  "apiKey": "cerb_xxxxxxxxxxxxxxxxx",
  "warning": "Store this API key securely. It will not be shown again.",
  "message": "Tenant 'My Organization' created successfully with master API key."
}
```

**IMPORTANT**:
- Copy the `apiKey` value immediately - you won't see it again!
- The bootstrap token is in `appsettings.json` - **CHANGE IT IN PRODUCTION** via environment variable
- After bootstrap, you can create additional tenants using the master API key

### Alternative: Create API Key + Tenant Together

After bootstrap, you can create new tenants by providing `tenantName` instead of `tenantId`:

```http
POST /api-keys
Content-Type: application/json

{
  "name": "Another Org API Key",
  "tenantName": "Another Organization",
  "projectId": null,
  "expiresAt": null
}
```

This creates both the tenant and the API key in one atomic operation!

### 3. Create a Project (requires API key)
```http
POST /tenants/{tenantId}/projects
Authorization: Bearer cerb_xxxxxxxxxxxxxxxxx
Content-Type: application/json

{
  "name": "Production App",
  "description": "Production environment secrets",
  "environment": "PRODUCTION"
}
```

### 4. Add Secrets (Animas) to the Project
```http
POST /tenants/{tenantId}/projects/{projectId}/animas
Authorization: Bearer cerb_xxxxxxxxxxxxxxxxx
Content-Type: application/json

{
  "definition": "DATABASE_URL",
  "value": "postgresql://user:pass@localhost:5432/mydb",
  "description": "Main database connection string",
  "environment": "PRODUCTION"
}
```

### 5. Retrieve a Secret
```http
GET /tenants/{tenantId}/projects/{projectId}/animas/DATABASE_URL
Authorization: Bearer cerb_xxxxxxxxxxxxxxxxx
```

## Complete API Reference

### Tenants
- `POST /tenants` - Create a new tenant
- `GET /tenants` - Get tenants (returns tenant associated with API key)
- `GET /tenants/{id}` - Get specific tenant
- `GET /tenants/{id}/projects` - Get all projects for a tenant

### Projects
- `POST /tenants/{tenantId}/projects` - Create a new project
- `GET /tenants/{tenantId}/projects/{projectId}` - Get specific project
- `GET /tenants/{tenantId}/projects/{projectId}/animas` - Get all secrets for a project

### Animas (Secrets)
- `POST /tenants/{tenantId}/projects/{projectId}/animas` - Create a secret
- `GET /tenants/{tenantId}/projects/{projectId}/animas/{definition}` - Get specific secret
- `GET /tenants/{tenantId}/projects/{projectId}/animas/environment/{env}` - Filter secrets by environment
- `PUT /tenants/{tenantId}/projects/{projectId}/animas/{animaId}` - Update a secret's value
- `DELETE /tenants/{tenantId}/projects/{projectId}/animas/{animaId}` - Delete a secret

### API Keys
- `POST /api-keys` - Create a new API key
- `GET /api-keys/tenant/{tenantId}` - List all API keys for a tenant
- `GET /api-keys/{id}` - Get specific API key details
- `DELETE /api-keys/{id}` - Revoke an API key

## Security Features

- **API Key Authentication**: All secret access requires valid API key
- **Cryptographically Secure Keys**: 256-bit random keys generated with `RandomNumberGenerator`
- **Hashed Storage**: Keys stored as SHA-256 hashes, never plaintext
- **Scoped Access**:
  - Tenant-scoped keys: Access all projects in a tenant
  - Project-scoped keys: Access only specific project
- **Audit Logging**: Table ready for tracking all secret access
- **Key Expiration**: Optional expiration dates on API keys
- **Key Revocation**: Instantly disable compromised keys

## Environment Values

Valid environment values:
- `DEVELOPMENT`
- `STAGING`
- `PRODUCTION`

## Database Architecture

### Tech Stack
- **PostgreSQL 16** - Reliable, ACID-compliant database
- **Dapper** - Lightweight micro-ORM for type-safe SQL
- **No EF Core** - Direct SQL control for security-critical operations

### Tables
- `tenants` - Organizations/teams
- `projects` - Applications within tenants
- `animas` - Secrets within projects
- `api_keys` - Authentication keys
- `audit_logs` - Access tracking (ready for implementation)
- `users` - User management

## Connection Details

- **Host**: localhost
- **Port**: 5433
- **Database**: cerberus
- **Username**: cerberus_user
- **Password**: cerberus_password

## Notes

- All IDs are UUIDs (GUIDs)
- Timestamps are stored in UTC
- Unique constraint on (project_id, definition, environment) for animas
- Cascading deletes: Deleting a tenant deletes all its projects and secrets
