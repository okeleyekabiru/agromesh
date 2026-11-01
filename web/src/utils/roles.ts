// Role gates for the farm logo, invoice and vendor-bill screens. These only decide what to show:
// the API enforces the same rules and answers 403 otherwise.
import type { UserRoleLabel } from '@/types'

type MaybeRole = UserRoleLabel | string | null | undefined

/** Upload/replace/remove the logo, edit the business profile, create/edit/delete sales invoices. */
export function canManageFarm(role: MaybeRole): boolean {
  return role === 'Admin' || role === 'FarmManager'
}

/** Attach a vendor invoice to a received order (managers plus supervisors). */
export function canUploadVendorInvoice(role: MaybeRole): boolean {
  return canManageFarm(role) || role === 'Supervisor'
}

/** Mark a vendor invoice paid/unpaid or delete it. */
export function canManageVendorInvoice(role: MaybeRole): boolean {
  return canManageFarm(role)
}
