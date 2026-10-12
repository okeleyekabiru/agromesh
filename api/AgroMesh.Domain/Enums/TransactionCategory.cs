namespace AgroMesh.Domain.Enums;

public enum TransactionCategory
{
    Labour,
    Fertilizer,
    Chemicals,
    Fuel,
    Equipment,
    Maintenance,
    Transportation,
    Seeds,
    Sales,
    Other,
    // Added last so the numbers already stored for the other categories do not move.
    // VAT, stamp duty, levies and bank charges.
    TaxAndCharges
}
