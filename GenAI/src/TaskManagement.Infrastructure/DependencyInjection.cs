using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Users;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;

namespace TaskManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TaskManagement")
            ?? throw new InvalidOperationException("Connection string 'TaskManagement' is missing.");

        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        services.AddNpgsqlDataSource(connectionString);
        services.AddSingleton<DatabaseInitializer>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
