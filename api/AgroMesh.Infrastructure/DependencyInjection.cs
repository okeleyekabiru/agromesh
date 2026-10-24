using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Infrastructure.Persistence;
using AgroMesh.Infrastructure.Persistence.Repositories;
using AgroMesh.Infrastructure.Services;
using AgroMesh.Infrastructure.Services.Auth;
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

        services.AddHttpContextAccessor();

        var redisConnection = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection));
        }

        services.AddHttpClient("weather-client");
        services.AddHttpClient("claude-client");
        services.AddHttpClient("mono-client");
        services.AddHttpClient("copernicus-client");

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<IBoundarySubmissionRepository, BoundarySubmissionRepository>();
        services.AddScoped<IFarmRepository, FarmRepository>();
        services.AddScoped<IBlockRepository, BlockRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddScoped<IWorkerRoleRepository, WorkerRoleRepository>();
        services.AddScoped<IChemicalProductRepository, ChemicalProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryCategoryRepository, InventoryCategoryRepository>();
        services.AddScoped<IStockAdjustmentRequestRepository, StockAdjustmentRequestRepository>();
        services.AddScoped<IPriceChangeRequestRepository, PriceChangeRequestRepository>();
        services.AddScoped<ICoverageCompletionRepository, CoverageCompletionRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ISchedulingPredictionRepository, SchedulingPredictionRepository>();
        services.AddScoped<ICropAllocationRuleRepository, CropAllocationRuleRepository>();

        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<IGeocodingService, GeocodingService>();
        services.AddScoped<IClaudeSchedulingService, ClaudeSchedulingService>();
        services.AddScoped<ISatelliteService, Services.Satellite.CopernicusSatelliteService>();
        services.AddScoped<IBlockSatelliteRepository, BlockSatelliteRepository>();
        services.AddScoped<SatelliteRefresher>();
        services.AddScoped<IInventoryDeductionService, InventoryDeductionService>();
        services.AddScoped<IOrderInvoiceService, OrderInvoiceService>();
        services.AddScoped<IFinancialCategorizationService, FinancialCategorizationService>();
        services.AddScoped<MonoApiService>();
        services.AddScoped<IMonoBankService, MonoApiService>();

        services.AddSingleton<BackgroundJobQueue>();
        services.AddSingleton<IBackgroundJobQueue>(sp => sp.GetRequiredService<BackgroundJobQueue>());
        services.AddSingleton<DailyBankSyncRunner>();
        services.AddSingleton<Services.Satellite.DailySatelliteRefreshRunner>();
        services.AddHostedService<SchedulingBackgroundService>();
        services.AddHostedService<DailyBankSyncService>();
        services.AddHostedService<Services.Satellite.DailySatelliteRefreshService>();

        return services;
    }
}
