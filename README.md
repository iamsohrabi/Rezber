<h1><img src="src/Rezber.Web/wwwroot/grapes.svg" alt="" width="40" height="40" /> Rezber</h1>

Rezber is an open-source package catalog and publishing portal built with ASP.NET Core. It keeps package metadata, user accounts, and catalog data in MongoDB, and integrates with Sonatype Nexus Repository for package artifacts and repository cataloging.

The project is intended to grow into a maintainable package-management platform. This README describes the capabilities and setup currently present in the repository; it does not imply that every feature is production-ready.

## What Rezber provides

- A web interface for browsing packages, viewing package details, and managing a developer account.
- Package metadata creation and management, with ownership checks for updates and deletion.
- API-token management and token-authenticated package publishing.
- Nexus Repository integration for package artifact upload and download.
- An administrator interface for user, package, audit, storage, and Nexus operations.
- A searchable MongoDB catalog of Nexus components, populated through an administrator-triggered synchronization.
- ASP.NET Core Identity backed by MongoDB, with administrator and developer roles.

## Architecture

| Project | Responsibility |
| --- | --- |
| `src/Rezber.Web` | ASP.NET Core MVC application, web pages, controllers, authentication, and HTTP API endpoints |
| `src/Rezber.Services` | Application workflows and integrations, including package and Nexus services |
| `src/Rezber.Domain` | Domain models and identity types |
| `src/Rezber.Core` | Shared abstractions, MongoDB infrastructure, settings, and common types |
| `test` | Automated tests |

MongoDB is the source of truth for Rezber's package metadata and operational records. Nexus Repository stores the package artifacts and can be synchronized into Rezber's separate `nexusCatalog` collection for searching.

## Requirements

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) (the repository pins the .NET 10 SDK in `global.json`)
- MongoDB, reachable by the web application
- A Nexus Repository instance for artifact publishing, downloads, and Nexus catalog integration

Nexus is configured separately from MongoDB. If you only want to run the web application without Nexus, disable the Nexus integration in configuration. Artifact operations and Nexus-specific features require a working Nexus instance.

## Getting started

### 1. Clone the repository

```bash
git clone https://github.com/iamsohrabi/Rezber.git
cd Rezber
```

### 2. Configure MongoDB and Nexus

The application reads configuration from `src/Rezber.Web/appsettings.json`, environment-specific settings, environment variables, and ASP.NET Core User Secrets. Use User Secrets locally, or your deployment platform's secret manager in production; do not commit passwords, API keys, or production connection strings.

Initialize User Secrets for the web project:

```bash
dotnet user-secrets init --project src/Rezber.Web/Rezber.Web.csproj
```

For a local MongoDB instance with no authentication, configure:

```bash
dotnet user-secrets set "MongoDbSettings:Host" "localhost" --project src/Rezber.Web/Rezber.Web.csproj
dotnet user-secrets set "MongoDbSettings:Port" "27017" --project src/Rezber.Web/Rezber.Web.csproj
dotnet user-secrets set "MongoDbSettings:User" "" --project src/Rezber.Web/Rezber.Web.csproj
dotnet user-secrets set "MongoDbSettings:Password" "" --project src/Rezber.Web/Rezber.Web.csproj
dotnet user-secrets set "MongoDbSettings:DatabaseName" "Rezber" --project src/Rezber.Web/Rezber.Web.csproj
dotnet user-secrets set "NexusSettings:Enabled" "false" --project src/Rezber.Web/Rezber.Web.csproj
```

For an authenticated MongoDB deployment, set `MongoDbSettings:User` and `MongoDbSettings:Password` to credentials with access to the configured database. When Nexus is enabled, configure its absolute base URL and credentials as well:

```bash
dotnet user-secrets set "NexusSettings:Enabled" "true" --project src/Rezber.Web/Rezber.Web.csproj
dotnet user-secrets set "NexusSettings:BaseUrl" "https://nexus.example.com" --project src/Rezber.Web/Rezber.Web.csproj
dotnet user-secrets set "NexusSettings:Username" "rezber-service" --project src/Rezber.Web/Rezber.Web.csproj
dotnet user-secrets set "NexusSettings:Password" "<your-nexus-password>" --project src/Rezber.Web/Rezber.Web.csproj
dotnet user-secrets set "NexusSettings:Repository" "nuget-hosted" --project src/Rezber.Web/Rezber.Web.csproj
```

Configuration values can also be supplied as environment variables using ASP.NET Core's double-underscore syntax, for example `MongoDbSettings__Host` and `NexusSettings__BaseUrl`.

### 3. Build and run

From the repository root:

```bash
dotnet restore Rezber.sln
dotnet build Rezber.sln
dotnet run --project src/Rezber.Web/Rezber.Web.csproj
```

The included launch profiles use `http://localhost:5023` and `https://localhost:7201`. The application connects to MongoDB during startup.

### Initial administrator account

> **Security notice:** The current `IdentitySeed` implementation creates an administrator using credentials hard-coded in the source code. These credentials are not configurable through application settings. Do not expose a running instance publicly or use it with production data until you have replaced this bootstrap behavior with a secure, deployment-specific administrator setup. Never rely on publicly visible seed credentials.

## Features and endpoints

### Package API

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `GET` | `/api/packages` | Public | List packages; accepts package filters and sorting options |
| `GET` | `/api/packages/{id}` | Public | Get package metadata by ID |
| `GET` | `/api/packages/{packageName}/{version}/download` | Public | Download a package artifact from Nexus |
| `GET` | `/api/packages/stats` | Public | Get package language counts and total downloads |
| `POST` | `/api/packages` | Authenticated | Create package metadata |
| `PUT` | `/api/packages/{id}` | Authenticated | Update package metadata |
| `DELETE` | `/api/packages/{id}` | Authenticated | Delete package metadata |

Package metadata creation does not upload an artifact. To publish a package artifact, create an API token in your account and use the push endpoint:

```http
PUT /api/packages/push/{packageName}/{version}/{fileName}
X-Rezber-ApiKey: <your-api-token>
Content-Type: application/octet-stream

<raw package file bytes>
```

The push endpoint accepts the token in `X-Rezber-ApiKey` (also `X-Nexora-ApiKey` or `X-Api-Key` for compatibility) and currently limits request bodies to 100 MB. The artifact is sent to Nexus and package metadata is registered in Rezber. Treat API tokens as passwords and revoke tokens that are no longer needed.

### API tokens

The authenticated account endpoints under `/api/tokens` allow the current user to list, create, and revoke their API tokens. Tokens are also manageable through the account's CLI token page.

### Nexus Repository administration

The administrator-only Nexus manager is available under `/admin/nexus`. Its current operations include:

- Checking Nexus status and repository writability.
- Listing repositories and inspecting individual repositories.
- Searching Nexus components and assets, with pagination through Nexus continuation tokens.
- Viewing and deleting components or assets.
- Starting and stopping repository health checks.
- Synchronizing Nexus components into Rezber's searchable catalog.

Component and asset deletion and repository health checks change Nexus state; restrict administrator access accordingly.

The Nexus catalog search endpoint is `GET /api/nexus/packages`. It requires authentication and supports `q`, `repository`, `format`, `name`, `version`, `group`, `page`, and `pageSize` query parameters. The page size is capped at 100. A successful synchronization removes catalog entries that are no longer present in Nexus; an incomplete synchronization does not remove previously indexed entries.

The Nexus instance's own OpenAPI document is available at `<Nexus Base URL>/service/rest/swagger.json`. Nexus APIs and artifact behavior can vary by Nexus version and repository format.

## Configuration reference

| Section | Key settings | Purpose |
| --- | --- | --- |
| `MongoDbSettings` | `Host`, `Port`, `User`, `Password`, `DatabaseName` | MongoDB connection details |
| `NexusSettings` | `Enabled`, `BaseUrl`, `Username`, `Password`, `Repository`, `ApiKey` | Nexus Repository integration |
| `ServiceSettings` | `ServiceName` | Application service name |
| `AllowedHosts` | Host allow-list | ASP.NET Core host filtering |
| `Logging` | Log levels | Application logging verbosity |

Keep credentials out of committed configuration files. In production, use a managed secret store or deployment environment variables, set a restrictive `AllowedHosts` value, and serve the application over HTTPS.

## Tests

Run the test suite from the repository root:

```bash
dotnet test Rezber.sln
```

## Contributing

Contributions are welcome. Before opening a pull request:

1. Check existing issues and pull requests to avoid duplicating work.
2. Keep changes focused and follow the existing project structure and conventions.
3. Add or update tests for behavior changes.
4. Run `dotnet build Rezber.sln` and `dotnet test Rezber.sln`.
5. Describe the motivation, implementation, and any configuration or migration impact in the pull request.

Please do not include credentials, tokens, or private deployment data in issues, commits, or pull requests. For security-sensitive reports, avoid publishing exploit details in a public issue.

## License

Rezber is distributed under the [MIT License](LICENSE).
