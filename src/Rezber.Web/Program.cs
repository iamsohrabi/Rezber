using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Rezber.Core.MongoDB;
using Rezber.Core.Settings;
using Rezber.Core.UnitOfWork;
using Rezber.Domain.Identity;
using Rezber.Domain.Models;
using Rezber.Services.Features;
using Rezber.Web.Services;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var mongoDbSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>()
    ?? throw new InvalidOperationException("MongoDbSettings service not available.");

builder.Services.Configure<NexusSettings>(
    builder.Configuration.GetSection(nameof(NexusSettings)));

builder.Services.AddHttpClient<INexusRepositoryClient, NexusRepositoryClient>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<NexusSettings>>().Value;
    if (settings.Enabled && !Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri))
        throw new InvalidOperationException("NexusSettings:BaseUrl must be an absolute URL.");

    if (settings.Enabled)
        client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddScoped<INexusCatalogService, NexusCatalogService>();

builder.Services.AddHttpClient<IRepositoryReadmeService, RepositoryReadmeService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Rezber-Web/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});

builder.Services.AddHttpClient<IPackageImageService, PackageImageService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Rezber-Web/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});

builder.Services.AddMongoDbService()
    .AddRepository<AuditLog>("auditLogs")
    .AddRepository<Package>("packages")
    .AddRepository<ApiToken>("apiTokens")
    .AddRepository<StorageSnapshot>("storageSnapshots")
    .AddApplicationService()
    .AddIdentityService(mongoDbSettings)
    .AddAuthenticationService()
    .AddHttpContextAccessor()
    .AddMemoryCache();

//  Web
builder.Services.AddControllersWithViews();
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

var app = builder.Build();

await IdentitySeed.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
