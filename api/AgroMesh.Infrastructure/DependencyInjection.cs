using AgroMesh.Application.Interfaces;
using AgroMesh.Infrastructure.Persistence;
using AgroMesh.Infrastructure.Persistence.Repositories;
using AgroMesh.Infrastructure.Services;
using AgroMesh.Infrastructure.Services.Financial;
using AgroMesh.Infrastructure.Services.Scheduling;
using AgroMesh.Infrastructure.Services.Weather;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AgroMesh.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AgroMeshDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        var redisConnection = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection));
        }

        services.AddHttpClient("weather-client");
        services.AddHttpClient("claude-client");
        services.AddHttpClient("mono-client");

        services.AddScoped<IFarmRepository, FarmRepository>();
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<IBlockRepository, BlockRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddScoped<IWorkerRoleRepository, WorkerRoleRepository>();
        services.AddScoped<IChemicalProductRepository, ChemicalProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ISchedulingPredictionRepository, SchedulingPredictionRepository>();
        services.AddScoped<ICropAllocationRuleRepository, CropAllocationRuleRepository>();

        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<IClaudeSchedulingService, ClaudeSchedulingService>();
        services.AddScoped<IInventoryDeductionService, InventoryDeductionService>();
        services.AddScoped<IFinancialCategorizationService, FinancialCategorizationService>();
        services.AddScoped<MonoApiService>();
        services.AddScoped<IMonoBankService, MonoApiService>();

        services.AddSingleton<BackgroundJobQueue>();
        services.AddSingleton<IBackgroundJobQueue>(sp => sp.GetRequiredService<BackgroundJobQueue>());
        services.AddHostedService<SchedulingBackgroundService>();

        return services;
    }
}
