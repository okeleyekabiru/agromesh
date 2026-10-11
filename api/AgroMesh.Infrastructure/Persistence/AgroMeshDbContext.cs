using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence;

public class AgroMeshDbContext : DbContext
{
    public AgroMeshDbContext(DbContextOptions<AgroMeshDbContext> options) : base(options) { }

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
    public DbSet<SchedulingPrediction> SchedulingPredictions => Set<SchedulingPrediction>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgroMeshDbContext).Assembly);
    }
}
