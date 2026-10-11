namespace AgroMesh.Domain.Enums;

/// <summary>Where a bank transaction sits in the farm's books: money out, money earned, or the owner's own investment.</summary>
public enum TransactionKind
{
    Expense,
    Income,
    Capital
}
