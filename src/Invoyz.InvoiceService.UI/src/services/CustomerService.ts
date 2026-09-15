import type { HttpClient } from '@/core/http/HttpClient'
import type { Customer, CustomerWriteModel } from '@/domain/models'
import { HttpCrudRepository } from './CrudService'

export class CustomerService extends HttpCrudRepository<
  Customer,
  CustomerWriteModel,
  CustomerWriteModel
> {
  constructor(http: HttpClient) {
    super(http, '/Customers')
  }
}
