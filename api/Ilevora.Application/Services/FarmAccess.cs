using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;

namespace Ilevora.Application.Services;

/// <summary>
/// The tenancy and role checks shared by the logo, business profile, invoice and vendor-bill handlers.
/// A farm of another company is reported as not found (never as forbidden, so its existence is not revealed), and a
/// caller with no company sees no farm at all: unlike some older handlers this fails closed.
/// </summary>
public static class FarmAccess
{
    /// <summary>Admin (the company owner) and FarmManager run the farm: brand, profile, invoices, bills.</summary>
    public static bool CanManage(UserRole role) => role is UserRole.Admin or UserRole.FarmManager;

    /// <summary>Anyone who works the farm day to day may attach a vendor bill to an order.</summary>
    public static bool CanUpload(UserRole role) => role is UserRole.Admin or UserRole.FarmManager or UserRole.Supervisor;

    public static async Task<Farm> GetFarmAsync(IFarmRepository farms, ICurrentUserAccessor user, string farmId, CancellationToken ct)
    {
        var farm = await farms.GetByIdAsync(farmId, ct);
        if (farm is null || string.IsNullOrEmpty(user.CompanyId) || farm.CompanyId != user.CompanyId)
            throw new NotFoundException("Farm", farmId);
        return farm;
    }

    public static void RequireManager(ICurrentUserAccessor user, string what)
    {
        if (!CanManage(user.Role))
            throw new ForbiddenException($"Only an admin or farm manager can {what}.");
    }
}
