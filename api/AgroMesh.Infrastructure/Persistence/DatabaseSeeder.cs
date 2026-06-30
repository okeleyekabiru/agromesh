using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AgroMeshDbContext db)
    {
        if (await db.Farms.AnyAsync()) return; // idempotent

        var farm = Farm.Create("Adeyemi Oil Palm & Maize Plantation", "Oyo State, Nigeria", 45.0, "owner_001", AreaUnit.Acres, id: "farm_001");
        farm.SetCoordinates(7.3775, 3.9470); // Oyo State approx
        db.Farms.Add(farm);
        await db.SaveChangesAsync();

        // Worker roles (rates live here)
        var supervisor = WorkerRole.Create(farm.Id, "Field Supervisor", WorkerType.Employee, 3500m);
        var assistantSupervisor = WorkerRole.Create(farm.Id, "Assistant Supervisor", WorkerType.Employee, 2800m);
        var farmWorker = WorkerRole.Create(farm.Id, "Farm Worker", WorkerType.Employee, 2200m);
        var contractor = WorkerRole.Create(farm.Id, "Contractor", WorkerType.Contractor, 4000m);
        db.WorkerRoles.AddRange(supervisor, assistantSupervisor, farmWorker, contractor);
        await db.SaveChangesAsync();

        // Workers reference roles, not rates
        var ibrahim = Worker.Create(farm.Id, "Ibrahim Musa", "IM", supervisor.Id, WorkerType.Employee);
        var tunde = Worker.Create(farm.Id, "Tunde Adeyemi", "TA", assistantSupervisor.Id, WorkerType.Employee);
        var emeka = Worker.Create(farm.Id, "Emeka Johnson", "EJ", farmWorker.Id, WorkerType.Employee);
        var fatima = Worker.Create(farm.Id, "Fatima Bello", "FB", farmWorker.Id, WorkerType.Employee);
        var chukwu = Worker.Create(farm.Id, "Chukwu Obi", "CO", contractor.Id, WorkerType.Contractor);
        db.Workers.AddRange(ibrahim, tunde, emeka, fatima, chukwu);
        await db.SaveChangesAsync();

        // Sections — multi-crop, with bounding-box GeoJSON in Oyo State
        var sectionA = Section.Create(farm.Id, "Section A", [new SectionCrop(CropType.OilPalm, 12.0, 100)], 12.0,
            BoundingBoxGeoJson(7.378, 3.945, 7.382, 3.949));
        sectionA.SetBoundary(BoundingBoxGeoJson(7.378, 3.945, 7.382, 3.949), 7.380, 3.947);

        var sectionB = Section.Create(farm.Id, "Section B", [new SectionCrop(CropType.Maize, 8.0, 100)], 8.0);
        sectionB.SetBoundary(BoundingBoxGeoJson(7.382, 3.945, 7.385, 3.948), 7.3835, 3.9465);

        var sectionC = Section.Create(farm.Id, "Section C", [new SectionCrop(CropType.OilPalm, 15.0, 100)], 15.0);
        sectionC.SetBoundary(BoundingBoxGeoJson(7.375, 3.949, 7.380, 3.953), 7.3775, 3.951);

        var sectionD = Section.Create(farm.Id, "Section D",
            [new SectionCrop(CropType.OilPalm, 5.0, 50), new SectionCrop(CropType.Maize, 5.0, 50)], 10.0);
        sectionD.SetBoundary(BoundingBoxGeoJson(7.380, 3.949, 7.384, 3.953), 7.382, 3.951);

        db.Sections.AddRange(sectionA, sectionB, sectionC, sectionD);
        await db.SaveChangesAsync();

        // Blocks
        db.Blocks.AddRange(
            Block.Create(sectionA.Id, farm.Id, "Block A1", 6.0, treeCount: 480),
            Block.Create(sectionA.Id, farm.Id, "Block A2", 6.0, treeCount: 480),
            Block.Create(sectionB.Id, farm.Id, "Block B1", 8.0),
            Block.Create(sectionC.Id, farm.Id, "Block C1", 7.5, treeCount: 600),
            Block.Create(sectionC.Id, farm.Id, "Block C2", 7.5, treeCount: 600),
            Block.Create(sectionD.Id, farm.Id, "Block D1", 10.0)
        );
        await db.SaveChangesAsync();
        var blockA1 = await db.Blocks.FirstAsync(b => b.SectionId == sectionA.Id);
        var blockB1 = await db.Blocks.FirstAsync(b => b.SectionId == sectionB.Id);

        // Chemical products registry
        var glyphosate = ChemicalProduct.Create("Glyphosate 360", ChemicalCategory.Herbicide, 60, "Candel Company");
        var atrazine = ChemicalProduct.Create("Atrazine 500SC", ChemicalCategory.Herbicide, 75, "Saro Agrosciences");
        var paraquat = ChemicalProduct.Create("Paraquat", ChemicalCategory.Herbicide, 45);
        var lambda = ChemicalProduct.Create("Lambda-cyhalothrin", ChemicalCategory.Insecticide, 30);
        var chlorpyrifos = ChemicalProduct.Create("Chlorpyrifos", ChemicalCategory.Insecticide, 30);
        var mancozeb = ChemicalProduct.Create("Mancozeb", ChemicalCategory.Fungicide, 21);
        var glufosinate = ChemicalProduct.Create("Glufosinate-ammonium", ChemicalCategory.Herbicide, 60);
        db.ChemicalProducts.AddRange(glyphosate, atrazine, paraquat, lambda, chlorpyrifos, mancozeb, glufosinate);
        await db.SaveChangesAsync();

        // Inventory items (Diesel deliberately below threshold)
        var glyphosateStock = InventoryItem.Create(farm.Id, "Glyphosate 360", "L", InventoryCategory.Chemical, 120, 30, glyphosate.Id);
        var atrazineStock = InventoryItem.Create(farm.Id, "Atrazine 500SC", "L", InventoryCategory.Chemical, 80, 20, atrazine.Id);
        var npk = InventoryItem.Create(farm.Id, "NPK 15-15-15", "kg", InventoryCategory.Fertilizer, 2500, 500);
        var urea = InventoryItem.Create(farm.Id, "Urea 46%", "kg", InventoryCategory.Fertilizer, 1800, 400);
        var maizeSeed = InventoryItem.Create(farm.Id, "Maize Hybrid Seeds", "kg", InventoryCategory.Seed, 150, 50);
        var diesel = InventoryItem.Create(farm.Id, "Diesel", "L", InventoryCategory.Fuel, 85, 100);
        var chains = InventoryItem.Create(farm.Id, "Chainsaw Chains", "units", InventoryCategory.SpareParts, 6, 2);
        var sprayers = InventoryItem.Create(farm.Id, "Knapsack Sprayers", "units", InventoryCategory.Equipment, 8, 2);
        db.InventoryItems.AddRange(glyphosateStock, atrazineStock, npk, urea, maizeSeed, diesel, chains, sprayers);
        await db.SaveChangesAsync();

        // Seeded activities — one weeding, one spraying (auto-deducts Glyphosate)
        var weedingActivity = ActivityLog.Create(
            farm.Id, sectionA.Id, blockA1.Id, ActivityType.Weeding, DateTime.UtcNow.AddDays(-10),
            workers:
            [
                new WorkerLog(emeka.Id, emeka.Name, emeka.Type, 8, farmWorker.DailyRate),
                new WorkerLog(fatima.Id, fatima.Name, fatima.Type, 8, farmWorker.DailyRate)
            ],
            inputs: [],
            supervisorId: ibrahim.Id,
            notes: "Routine weeding — Block A1",
            workedAreaGeoJson: BoundingBoxGeoJson(7.378, 3.945, 7.380, 3.947),
            workedHectares: 6.0);

        var sprayingActivity = ActivityLog.Create(
            farm.Id, sectionB.Id, blockB1.Id, ActivityType.Spraying, DateTime.UtcNow.AddDays(-5),
            workers: [new WorkerLog(chukwu.Id, chukwu.Name, chukwu.Type, 6, contractor.DailyRate)],
            inputs: [new InputUsed("Glyphosate 360", 15, "L")],
            supervisorId: tunde.Id,
            notes: "Pre-emergent herbicide application",
            chemicalProductId: glyphosate.Id,
            chemicalProductName: glyphosate.Name,
            workedAreaGeoJson: BoundingBoxGeoJson(7.382, 3.945, 7.385, 3.948),
            workedHectares: 8.0);

        db.ActivityLogs.AddRange(weedingActivity, sprayingActivity);
        await db.SaveChangesAsync();

        // Stock movement from the spraying activity's chemical usage
        glyphosateStock.DeductStock(15);
        db.InventoryItems.Update(glyphosateStock);
        db.StockMovements.Add(StockMovement.CreateOut(farm.Id, glyphosateStock.Id, 15, sprayingActivity.Id, "Auto-deducted: spraying activity"));
        await db.SaveChangesAsync();

        // Allocation rules
        db.CropAllocationRules.AddRange(
            CropAllocationRule.Create(farm.Id, TransactionCategory.Fertilizer, new Dictionary<string, double> { ["OilPalm"] = 0.6, ["Maize"] = 0.4 }),
            CropAllocationRule.Create(farm.Id, TransactionCategory.Chemicals, new Dictionary<string, double> { ["OilPalm"] = 0.5, ["Maize"] = 0.5 }),
            CropAllocationRule.Create(farm.Id, TransactionCategory.Labour, new Dictionary<string, double> { ["OilPalm"] = 0.65, ["Maize"] = 0.35 })
        );
        await db.SaveChangesAsync();

        // Sample transactions
        db.Transactions.AddRange(
            Transaction.Create(farm.Id, "mono_tx_001", "DIESEL PURCHASE - NIPCO STATION IBADAN", -45000m, DateTime.UtcNow.AddDays(-3), TransactionCategory.Fuel, ConfidenceLevel.High, CategorizationSource.Dictionary),
            Transaction.Create(farm.Id, "mono_tx_002", "FERTILIZER NPK 15-15-15 BULK ORDER", -380000m, DateTime.UtcNow.AddDays(-7), TransactionCategory.Fertilizer, ConfidenceLevel.High, CategorizationSource.Dictionary),
            Transaction.Create(farm.Id, "mono_tx_003", "FRESH FRUIT BUNCH SALE - PALM OIL MILL", 1250000m, DateTime.UtcNow.AddDays(-2), TransactionCategory.Sales, ConfidenceLevel.Medium, CategorizationSource.Pattern)
        );
        await db.SaveChangesAsync();
    }

    private static string BoundingBoxGeoJson(double lat1, double lng1, double lat2, double lng2)
    {
        return $@"{{""type"":""Polygon"",""coordinates"":[[[{lng1},{lat1}],[{lng2},{lat1}],[{lng2},{lat2}],[{lng1},{lat2}],[{lng1},{lat1}]]]}}";
    }
}
