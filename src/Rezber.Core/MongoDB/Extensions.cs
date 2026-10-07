using Rezber.Core.Domain;
using Rezber.Core.Settings;
using Rezber.Core.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Rezber.Core.MongoDB;

public static class Extentions
{
    /// <summary>
    /// Registers MongoDB services in the DI container.
    /// </summary>
    public static IServiceCollection AddMongoDbService(this IServiceCollection services)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
        BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));

        services.AddSingleton(provider =>
        {
            var configurations = provider.GetRequiredService<IConfiguration>();

            var serviceSettings = configurations
                .GetSection(nameof(ServiceSettings))
                .Get<ServiceSettings>()
                ?? throw new InvalidOperationException("ServiceSettings service not available.");

            var mongoDbSettings = GetMongoDbSettings(configurations);

            var mongoUrl = new MongoUrlBuilder
            {
                Server = new MongoServerAddress(
                    mongoDbSettings.Host,
                    mongoDbSettings.Port),

                DatabaseName = mongoDbSettings.DatabaseName,

                Username = mongoDbSettings.User,
                Password = mongoDbSettings.Password,

                AuthenticationSource = "admin"
            };

            var mongoSettings = MongoClientSettings.FromUrl(mongoUrl.ToMongoUrl());

            mongoSettings.ApplicationName = serviceSettings.ServiceName;
            mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(5);
            mongoSettings.SocketTimeout = TimeSpan.FromSeconds(10);
            mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            mongoSettings.MaxConnectionPoolSize = 10;
            mongoSettings.MinConnectionPoolSize = 1;
            mongoSettings.MaxConnectionIdleTime = TimeSpan.FromMinutes(2);
            mongoSettings.WaitQueueTimeout = TimeSpan.FromSeconds(2);

            var mongoClient = new MongoClient(mongoSettings);

            return mongoClient.GetDatabase(mongoDbSettings.DatabaseName);
        });

        return services;
    }

    /// <summary>
    /// Reads MongoDbSettings from environment variables first, then from appsettings.json
    /// </summary>
    private static MongoDbSettings GetMongoDbSettings(IConfiguration configuration)
    {
        var host = Environment.GetEnvironmentVariable("MongoDbSettings_Host");
        var portStr = Environment.GetEnvironmentVariable("MongoDbSettings_Port");
        var user = Environment.GetEnvironmentVariable("MongoDbSettings_User");
        var password = Environment.GetEnvironmentVariable("MongoDbSettings_Password");
        var databaseName = Environment.GetEnvironmentVariable("MongoDbSettings_DatabaseName");

        if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(portStr))
        {
            return new MongoDbSettings
            {
                Host = host,
                Port = int.Parse(portStr),
                User = user ?? string.Empty,
                Password = password ?? string.Empty,
                DatabaseName = databaseName ?? string.Empty
            };
        }

        return configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>()
               ?? throw new InvalidOperationException("MongoDbSettings service not available.");
    }

    /// <summary>
    /// Adds a scoped MongoDB repository service for the given entity type T and ID type TID.
    /// </summary>
    public static IServiceCollection AddRepository<T, TID>(
        this IServiceCollection services,
        string collectionName)
        where T : AuditAggregate<TID>
    {
        services.AddScoped<IRepository<T, TID>>(serviceProvider =>
        {
            var database = serviceProvider.GetRequiredService<IMongoDatabase>();

            return new MongoRepository<T, TID>(database, collectionName);
        });

        return services;
    }

    /// <summary>
    /// Adds a scoped MongoDB repository service for the given entity type T with ID type Guid.
    /// </summary>
    public static IServiceCollection AddRepository<T>(
        this IServiceCollection services,
        string collectionName)
        where T : AuditAggregate<Guid>
    {
        services.AddScoped<IRepository<T>>(serviceProvider =>
        {
            var database = serviceProvider.GetRequiredService<IMongoDatabase>();

            return new MongoRepository<T>(database, collectionName);
        });

        return services;
    }
}