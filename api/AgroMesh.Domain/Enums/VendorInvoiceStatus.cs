namespace AgroMesh.Domain.Enums;

/// <summary>Whether the farm has paid a bill a vendor sent it. Recorded by a person: it is not derived from bank transactions.</summary>
public enum VendorInvoiceStatus
{
    Unpaid,
    Paid
}
