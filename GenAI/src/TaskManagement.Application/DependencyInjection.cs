using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Tasks;
using TaskManagement.Application.Users;

namespace TaskManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
