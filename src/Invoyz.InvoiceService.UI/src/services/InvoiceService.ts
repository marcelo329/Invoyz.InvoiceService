import type { HttpClient } from '@/core/http/HttpClient'
import type { Invoice, InvoiceCreateModel, InvoiceUpdateModel } from '@/domain/models'
import { HttpCrudRepository } from './CrudService'

export class InvoiceService extends HttpCrudRepository<
  Invoice,
  InvoiceCreateModel,
  InvoiceUpdateModel
> {
  constructor(http: HttpClient) {
    super(http, '/Invoices')
  }
}
