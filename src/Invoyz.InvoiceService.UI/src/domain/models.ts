/**
 * Mirrors the API contracts in `Invoyz.InvoiceService.Contracts`.
 *
 * Read models carry an `Id`; write models deliberately do not — the API treats the
 * route id as authoritative and its update contracts have no `Id` property, so making
 * one unrepresentable here keeps the two sides honest.
 */
export type Guid = string

/** Every read model the API returns is identifiable and audited. */
export interface AuditedResource {
  readonly id: Guid
  readonly createdAt: string
  readonly lastModifiedAt: string | null
}

export interface Customer extends AuditedResource {
  readonly name: string
  readonly address: string
  readonly email: string
  readonly vatNumber: string
}

export interface CustomerWriteModel {
  name: string
  address: string
  email: string
  vatNumber: string
}

export interface Product extends AuditedResource {
  readonly name: string
  readonly description: string
  readonly unitPrice: number
  readonly taxRate: number
}

export interface ProductWriteModel {
  name: string
  description: string
  unitPrice: number
  taxRate: number
}

export const invoiceStatuses = ['Draft', 'Sent', 'Paid', 'Overdue'] as const
export type InvoiceStatus = (typeof invoiceStatuses)[number]

export interface InvoiceLine extends AuditedResource {
  readonly invoiceId: Guid
  readonly productId: Guid
  readonly quantity: number
  readonly unitPrice: number
  readonly taxRate: number
  /** Derived server-side; never sent on a write. */
  readonly lineTotal: number
  readonly lineTax: number
}

/**
 * `unitPrice` and `taxRate` are optional: omitting them tells the API to snapshot the
 * product's current values, which is why they are nullable rather than defaulted here.
 */
export interface InvoiceLineWriteModel {
  productId: Guid
  quantity: number
  unitPrice: number | null
  taxRate: number | null
}

export interface Invoice extends AuditedResource {
  readonly invoiceNumber: string
  readonly customerId: Guid
  readonly issueDate: string
  readonly dueDate: string
  readonly status: string
  readonly subTotal: number
  readonly taxTotal: number
  readonly grandTotal: number
  readonly lines: readonly InvoiceLine[]
}

/** Creating an invoice requires at least one line: the API rejects an empty array. */
export interface InvoiceCreateModel {
  invoiceNumber: string
  customerId: Guid
  issueDate: string
  dueDate: string
  status: string
  lines: InvoiceLineWriteModel[]
}

/** Updating an invoice never touches its lines: those are a separate sub-resource. */
export interface InvoiceUpdateModel {
  invoiceNumber: string
  customerId: Guid
  issueDate: string
  dueDate: string
  status: string
}

export interface PageRequest {
  page: number
  pageSize: number
}
