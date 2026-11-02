using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence;

public class IlevoraDbContext : DbContext
{
    public IlevoraDbContext(DbContextOptions<IlevoraDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<State> States => Set<State>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<BoundarySubmission> BoundarySubmissions => Set<BoundarySubmission>();
    public DbSet<WorkerRole> WorkerRoles => Set<WorkerRole>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CropAllocationRule> CropAllocationRules => Set<CropAllocationRule>();
    public DbSet<ChemicalProduct> ChemicalProducts => Set<ChemicalProduct>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockAdjustmentRequest> StockAdjustmentRequests => Set<StockAdjustmentRequest>();
    public DbSet<PriceChangeRequest> PriceChangeRequests => Set<PriceChangeRequest>();
    public DbSet<CoverageCompletion> CoverageCompletions => Set<CoverageCompletion>();
    public DbSet<BlockSatelliteReading> BlockSatelliteReadings => Set<BlockSatelliteReading>();
    public DbSet<SchedulingPrediction> SchedulingPredictions => Set<SchedulingPrediction>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<VendorInvoice> VendorInvoices => Set<VendorInvoice>();
    public DbSet<InvoiceCounter> InvoiceCounters => Set<InvoiceCounter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IlevoraDbContext).Assembly);
    }
}
