import type { HttpClient } from '@/core/http/HttpClient'
import type { Product, ProductWriteModel } from '@/domain/models'
import { HttpCrudRepository } from './CrudService'

export class ProductService extends HttpCrudRepository<
  Product,
  ProductWriteModel,
  ProductWriteModel
> {
  constructor(http: HttpClient) {
    super(http, '/Products')
  }
}
