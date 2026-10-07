# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 

# Getting Started
TODO: Guide users through getting your code up and running on their own system. In this section you can talk about:
1.	Installation process
2.	Software dependencies
3.	Latest releases
4.	API references

# Build and Test
TODO: Describe and show how to build your code and run the tests. 

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:
 
# Rezber

Rezber is a package catalog backed by MongoDB. Package metadata and Rezber
analytics remain in MongoDB, while Nexus Repository can be used as the artifact
registry for package files.

## Nexus Repository

The integration follows Nexus Repository's documented REST API under
`/service/rest/v1` and uses Basic Authentication when credentials are configured.
Enable it in `src/Rezber.Web/appsettings.json` or an environment-specific
configuration file:

```json
"NexusSettings": {
	"Enabled": true,
	"BaseUrl": "https://nexus.example.com",
	"Username": "rezber-service",
	"Password": "use-a-secret-store",
	"Repository": "rezber-hosted"
}
```

The admin-only endpoints are:

- `GET /admin/nexus/status`
- `GET /admin/nexus/writable`
- `GET /admin/nexus/repositories`
- `GET /admin/nexus/repositories/{repositoryName}`
- `GET /admin/nexus/search?q=<package-name>&continuationToken=<token>`
- `GET /admin/nexus/search/assets?q=<package-name>&continuationToken=<token>`
- `GET /admin/nexus/components/{id}` and `DELETE /admin/nexus/components/{id}`
- `GET /admin/nexus/assets/{id}` and `DELETE /admin/nexus/assets/{id}`
- `POST` or `DELETE /admin/nexus/repositories/{repositoryName}/health-check`

Search requests also support `repository`, `format`, `name`, `version`, and
`group`. The `continuationToken` returned by Nexus can be passed to retrieve
the next page. Component and asset deletion, as well as health checks, should
remain restricted to trusted administrators because they change Nexus state.

The instance-specific OpenAPI contract is available at
`<BaseUrl>/service/rest/swagger.json`. Artifact upload and download are
format-specific in Nexus; the current package create API accepts metadata only,
so it does not pretend to upload a missing package file.

The Nexus manager provides an administrator-only **Sync searchable catalog**
action. It imports every versioned component from every Nexus repository into
the separate MongoDB `nexusCatalog` collection, preserving the existing Rezber
package records. A completed sync removes entries that are no longer present;
an incomplete sync may update entries already scanned but does not remove
previous entries. Search the
MongoDB index through `GET /api/nexus/packages?q=<term>`, with optional
`repository`, `format`, `name`, `version`, `group`, `page`, and `pageSize`
filters. The page size is limited to 100.

## Build

```bash
dotnet build Rezber.sln
```