using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class Farm : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public double TotalHectares { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public string CompanyId { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = "NG";
    public string? StateId { get; private set; }
    public string? LgaFreetext { get; private set; }
    public FarmSetupStatus SetupStatus { get; private set; } = FarmSetupStatus.Draft;
    public AreaUnit PreferredAreaUnit { get; private set; } = AreaUnit.Hectares;
    public double? CentroidLat { get; private set; }
    public double? CentroidLng { get; private set; }

    // The farm's linked bank account at Mono. The id is permanent until the account is unlinked.
    public string? BankAccountId { get; private set; }
    public DateTime? BankLinkedAt { get; private set; }
    public DateTime? LastBankSyncAt { get; private set; }

    public List<Block> Blocks { get; private set; } = [];
    public List<WorkerRole> WorkerRoles { get; private set; } = [];
    public List<Worker> Workers { get; private set; } = [];
    public List<CropAllocationRule> AllocationRules { get; private set; } = [];
    public List<InventoryItem> InventoryItems { get; private set; } = [];
    public List<InventoryCategory> InventoryCategories { get; private set; } = [];

    private Farm() { }

    public static Farm Create(string name, string companyId, string ownerId, string? stateId = null, string? lgaFreetext = null, string countryCode = "NG", string? id = null)
    {
        return new Farm
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name,
            CompanyId = companyId,
            OwnerId = ownerId,
            StateId = stateId,
            LgaFreetext = lgaFreetext,
            CountryCode = countryCode,
        };
    }

    public void LinkBankAccount(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new Exceptions.DomainException("The bank did not return an account.");
        BankAccountId = accountId;
        BankLinkedAt = DateTime.UtcNow;
        LastBankSyncAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnlinkBankAccount()
    {
        BankAccountId = null;
        BankLinkedAt = null;
        LastBankSyncAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>The bank is synced once a day: due when linked and not synced in the last 24 hours.</summary>
    public bool BankSyncDue(DateTime nowUtc) =>
        BankAccountId is not null && (LastBankSyncAt is null || nowUtc - LastBankSyncAt.Value >= TimeSpan.FromHours(24));

    public void MarkBankSynced()
    {
        LastBankSyncAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCoordinates(double lat, double lng)
    {
        CentroidLat = lat;
        CentroidLng = lng;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAreaUnit(AreaUnit unit)
    {
        PreferredAreaUnit = unit;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string location, double totalHectares)
    {
        Name = name;
        Location = location;
        TotalHectares = totalHectares;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetLocation(string stateId, string? lgaFreetext)
    {
        StateId = stateId;
        LgaFreetext = lgaFreetext;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkConfigured()
    {
        SetupStatus = FarmSetupStatus.Configured;
        UpdatedAt = DateTime.UtcNow;
    }
}
