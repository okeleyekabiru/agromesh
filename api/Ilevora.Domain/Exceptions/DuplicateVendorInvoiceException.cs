namespace Ilevora.Domain.Exceptions;

/// <summary>Raised by the repository when the database refuses a vendor invoice because that vendor's invoice number is already on the farm.</summary>
public class DuplicateVendorInvoiceException : DomainException
{
    public DuplicateVendorInvoiceException() : base("This vendor invoice number has already been uploaded.") { }
}
