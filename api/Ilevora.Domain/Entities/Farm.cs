using Ilevora.Domain.Enums;

namespace Ilevora.Domain.Entities;

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

    // Satellite crop readings are refreshed once a day, and by hand no more than once an hour (the free quota is limited).
    public DateTime? LastSatelliteSyncAt { get; private set; }

    // The farm's brand logo: points at a StoredFile. LogoUpdatedAt doubles as the cache-busting key for clients.
    public string? LogoFileId { get; private set; }
    public DateTime? LogoUpdatedAt { get; private set; }
    public bool HasLogo => LogoFileId is not null;

    // Brand colours for invoices as "#RRGGBB". Null means "use each template's own colours".
    public string? BrandPrimaryColor { get; private set; }
    public string? BrandAccentColor { get; private set; }

    // How the farm presents itself to customers (printed on invoices). BankAccountName/Number/BankName are the account
    // customers pay into, not the Mono link above (BankAccountId).
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }
    public string? BankAccountName { get; private set; }
    public string? BankAccountNumber { get; private set; }
    public string? BankName { get; private set; }

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

    public bool SatelliteSyncDue(DateTime nowUtc, TimeSpan every) =>
        LastSatelliteSyncAt is null || nowUtc - LastSatelliteSyncAt.Value >= every;

    public void MarkSatelliteSynced()
    {
        LastSatelliteSyncAt = DateTime.UtcNow;
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

    public void SetLogo(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new Exceptions.DomainException("A logo needs a stored file.");
        LogoFileId = fileId;
        LogoUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearLogo()
    {
        LogoFileId = null;
        LogoUpdatedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Replaces the whole business profile. Blank values are cleared.</summary>
    public void SetBusinessProfile(string? address, string? phone, string? email, string? website,
        string? bankAccountName, string? bankAccountNumber, string? bankName,
        string? brandPrimaryColor = null, string? brandAccentColor = null)
    {
        var newAddress = Clean(address, 300, "Address");
        var newPhone = Clean(phone, 60, "Phone");
        var newEmail = Clean(email, 200, "Email");
        var newWebsite = Clean(website, 200, "Website");
        var newAccountName = Clean(bankAccountName, 150, "Bank account name");
        var newAccountNumber = Clean(bankAccountNumber, 40, "Bank account number");
        var newBankName = Clean(bankName, 100, "Bank name");
        var newPrimary = Utilities.BrandColor.Normalise(brandPrimaryColor, "Primary brand colour");
        var newAccent = Utilities.BrandColor.Normalise(brandAccentColor, "Accent brand colour");
        if (newEmail is not null && !Utilities.EmailAddress.IsPlausible(newEmail))
            throw new Exceptions.DomainException("The email address is not valid.");

        Address = newAddress;
        Phone = newPhone;
        Email = newEmail;
        Website = newWebsite;
        BankAccountName = newAccountName;
        BankAccountNumber = newAccountNumber;
        BankName = newBankName;
        BrandPrimaryColor = newPrimary;
        BrandAccentColor = newAccent;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? Clean(string? value, int max, string label)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > max)
            throw new Exceptions.DomainException($"{label} must be at most {max} characters.");
        return trimmed;
    }

    public void MarkConfigured()
    {
        SetupStatus = FarmSetupStatus.Configured;
        UpdatedAt = DateTime.UtcNow;
    }
}
