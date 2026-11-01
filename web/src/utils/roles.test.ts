import { describe, expect, it } from 'vitest'
import { canManageFarm, canManageVendorInvoice, canUploadVendorInvoice } from './roles'

describe('role gates', () => {
  it('lets only Admin and FarmManager manage the farm, logo and sales invoices', () => {
    expect(canManageFarm('Admin')).toBe(true)
    expect(canManageFarm('FarmManager')).toBe(true)
    expect(canManageFarm('Supervisor')).toBe(false)
    expect(canManageFarm('Member')).toBe(false)
    expect(canManageFarm(undefined)).toBe(false)
  })
  it('lets Supervisors upload vendor invoices but not pay or delete them', () => {
    expect(canUploadVendorInvoice('Supervisor')).toBe(true)
    expect(canUploadVendorInvoice('FarmManager')).toBe(true)
    expect(canUploadVendorInvoice('Member')).toBe(false)
    expect(canManageVendorInvoice('Supervisor')).toBe(false)
    expect(canManageVendorInvoice('Admin')).toBe(true)
  })
})
