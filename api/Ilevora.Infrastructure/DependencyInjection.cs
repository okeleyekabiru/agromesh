using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using Ilevora.Infrastructure.Persistence;
using Ilevora.Infrastructure.Persistence.Repositories;
using Ilevora.Infrastructure.Services;
using Ilevora.Infrastructure.Services.Auth;
using Ilevora.Infrastructure.Services.Email;
using Ilevora.Infrastructure.Services.Financial;
using Ilevora.Infrastructure.Services.Invoices;
using Ilevora.Infrastructure.Services.Scheduling;
using Ilevora.Infrastructure.Services.Weather;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Ilevora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<IlevoraDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddHttpContextAccessor();

        var redisConnection = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            // Redis is only a cache. AbortOnConnectFail=false keeps an unreachable Redis from throwing when the
            // multiplexer is first resolved; the cache calls already swallow errors and fall back to the source.
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(BuildRedisOptions(redisConnection)));
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
        services.AddScoped<ICropRepository, CropRepository>();
        services.AddScoped<IStockAdjustmentRequestRepository, StockAdjustmentRequestRepository>();
        services.AddScoped<IPriceChangeRequestRepository, PriceChangeRequestRepository>();
        services.AddScoped<ICoverageCompletionRepository, CoverageCompletionRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IStoredFileRepository, StoredFileRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IVendorInvoiceRepository, VendorInvoiceRepository>();
        services.AddScoped<ISchedulingPredictionRepository, SchedulingPredictionRepository>();
        services.AddScoped<ICropAllocationRuleRepository, CropAllocationRuleRepository>();

        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Transactional email (Resend). IEmailSender writes to the EmailOutbox table; EmailDispatchService delivers from there in the background.
        services.Configure<ResendOptions>(o =>
        {
            o.ApiKey = config["Resend:ApiKey"];
            o.FromAddress = config["Resend:FromAddress"];
            o.Enabled = bool.TryParse(config["Resend:Enabled"], out var enabled) && enabled;
        });
        services.Configure<AppOptions>(o =>
        {
            var url = config["App:WebBaseUrl"];
            if (!string.IsNullOrWhiteSpace(url)) o.WebBaseUrl = url;
        });
        services.AddHttpClient<ResendEmailTransport>(c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddScoped<IEmailSender, OutboxEmailSender>();
        services.AddScoped<IEmailOutboxStore, EmailOutboxStore>();
        services.AddScoped<IEmailTransport>(sp => sp.GetRequiredService<ResendEmailTransport>());
        services.AddScoped<EmailOutboxProcessor>();
        services.AddSingleton<IAppUrls, AppUrls>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<EmailDispatchService>();

        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<IGeocodingService, GeocodingService>();
        services.AddScoped<IClaudeSchedulingService, ClaudeSchedulingService>();
        services.AddScoped<ISatelliteService, Services.Satellite.CopernicusSatelliteService>();
        services.AddScoped<IBlockSatelliteRepository, BlockSatelliteRepository>();
        services.AddScoped<SatelliteRefresher>();
        services.AddScoped<IInventoryDeductionService, InventoryDeductionService>();
        services.AddScoped<IOrderInvoiceService, OrderInvoiceService>();
        services.AddSingleton<IImageInspector, QuestPdfImageInspector>();
        // Every IInvoiceTemplateRenderer registered here is an invoice template the API offers; add a layout by adding a line.
        services.AddSingleton<IInvoiceTemplateRenderer, NgfInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRenderer, BandInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRenderer, SidebarInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRenderer, MinimalInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRenderer, CalloutInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRenderer, CardsInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRenderer, LedgerInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRenderer, AngleInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRenderer, AnchorInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRenderer, DuoInvoiceTemplate>();
        services.AddSingleton<IInvoiceTemplateRegistry, InvoiceTemplateRegistry>();
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

    // Accepts either a StackExchange.Redis string ("host:6379,password=...") or the redis:// / rediss:// URL
    // that hosts like Render provide.
    private static ConfigurationOptions BuildRedisOptions(string connection)
    {
        ConfigurationOptions options;
        // "redis:6379" (a host that happens to be called redis, as in docker-compose) also parses as an absolute URI
        // with scheme "redis" and an empty host, so require a real host before treating it as a URL.
        if (Uri.TryCreate(connection, UriKind.Absolute, out var uri) && (uri.Scheme is "redis" or "rediss") && uri.Host.Length > 0)
        {
            options = new ConfigurationOptions { Ssl = uri.Scheme == "rediss" };
            options.EndPoints.Add(uri.Host, uri.Port > 0 ? uri.Port : 6379);

            var userInfo = uri.UserInfo.Split(':', 2);
            if (userInfo.Length == 2)
            {
                if (userInfo[0].Length > 0) options.User = Uri.UnescapeDataString(userInfo[0]);
                options.Password = Uri.UnescapeDataString(userInfo[1]);
            }
            else if (userInfo[0].Length > 0) options.Password = Uri.UnescapeDataString(userInfo[0]);
        }
        else
        {
            options = ConfigurationOptions.Parse(connection);
        }

        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 3000;
        return options;
    }
}
