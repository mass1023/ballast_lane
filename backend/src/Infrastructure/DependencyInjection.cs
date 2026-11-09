using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecordLabel.Application.Abstractions;
using RecordLabel.Infrastructure.Persistence;
using RecordLabel.Infrastructure.Persistence.Repositories;
using RecordLabel.Infrastructure.Security;

namespace RecordLabel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RecordLabel")
            ?? throw new InvalidOperationException("Connection string 'RecordLabel' is not configured.");

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .Validate(options => options.Secret.Length >= 32, "Jwt:Secret must be at least 32 characters (HMAC-SHA256 key).")
            .ValidateOnStart();

        services.AddNpgsqlDataSource(connectionString);
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<DataSeeder>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IReleaseRepository, ReleaseRepository>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
