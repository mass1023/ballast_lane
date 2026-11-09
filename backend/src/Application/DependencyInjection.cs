using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecordLabel.Application.Releases;
using RecordLabel.Application.Users;

namespace RecordLabel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IReleaseService, ReleaseService>();
        services.AddScoped<IUserService, UserService>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
