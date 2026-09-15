import type { HttpClient } from '@/core/http/HttpClient'
import type { Guid, InvoiceLine, InvoiceLineWriteModel, PageRequest } from '@/domain/models'
import type { CrudRepository } from './CrudService'
import { HttpCrudRepository } from './CrudService'

/**
 * Lines are a sub-resource: every path is scoped by its parent invoice, so this is a
 * factory rather than a singleton service. Binding the invoice id once keeps callers
 * from having to thread it through every method, and keeps the repository interface
 * identical to the top-level resources (LSP: a bound line repository is usable
 * anywhere a CrudRepository is expected).
 */
export type InvoiceLineRepository = CrudRepository<
  InvoiceLine,
  InvoiceLineWriteModel,
  InvoiceLineWriteModel
>

export class InvoiceLineServiceFactory {
  constructor(private readonly http: HttpClient) {}

  forInvoice(invoiceId: Guid): InvoiceLineRepository {
    return new HttpCrudRepository<InvoiceLine, InvoiceLineWriteModel, InvoiceLineWriteModel>(
      this.http,
      `/Invoices/${invoiceId}/Lines`,
    )
  }
}

export type { Guid, InvoiceLine, InvoiceLineWriteModel, PageRequest }
