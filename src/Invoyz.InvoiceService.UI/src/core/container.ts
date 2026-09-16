import type { App, InjectionKey } from 'vue'
import { inject } from 'vue'

import { loadAppConfig, type AppConfig } from '@/config/appConfig'
import { AxiosHttpClient } from '@/core/http/AxiosHttpClient'
import type { HttpClient } from '@/core/http/HttpClient'
import { ApiInvoicePdfGenerator } from '@/pdf/ApiInvoicePdfGenerator'
import { type InvoicePdfGenerator } from '@/pdf/InvoicePdfGenerator'
import { CustomerService } from '@/services/CustomerService'
import { InvoiceLineServiceFactory } from '@/services/InvoiceLineService'
import { InvoiceService } from '@/services/InvoiceService'
import { ProductService } from '@/services/ProductService'

/**
 * Composition root.
 *
 * Every dependency is constructed once here and handed down through Vue's provide /
 * inject. Components ask for an interface by key and never construct a service, so a
 * test can mount a view over fakes by providing different values for the same keys.
 */
export interface Services {
  readonly config: AppConfig
  readonly http: HttpClient
  readonly customers: CustomerService
  readonly products: ProductService
  readonly invoices: InvoiceService
  readonly invoiceLines: InvoiceLineServiceFactory
  readonly invoicePdf: InvoicePdfGenerator
}

export const appConfigKey: InjectionKey<AppConfig> = Symbol('appConfig')
export const customerServiceKey: InjectionKey<CustomerService> = Symbol('customerService')
export const productServiceKey: InjectionKey<ProductService> = Symbol('productService')
export const invoiceServiceKey: InjectionKey<InvoiceService> = Symbol('invoiceService')
export const invoiceLineServiceKey: InjectionKey<InvoiceLineServiceFactory> =
  Symbol('invoiceLineServiceFactory')
export const invoicePdfGeneratorKey: InjectionKey<InvoicePdfGenerator> =
  Symbol('invoicePdfGenerator')

export function createServices(config: AppConfig = loadAppConfig()): Services {
  const http = new AxiosHttpClient(config.api)

  return {
    config,
    http,
    customers: new CustomerService(http),
    products: new ProductService(http),
    invoices: new InvoiceService(http),
    invoiceLines: new InvoiceLineServiceFactory(http),
    // Swap for NotImplementedInvoicePdfGenerator to disable the feature without
    // touching the view.
    invoicePdf: new ApiInvoicePdfGenerator(http),
  }
}

export function installServices(app: App, services: Services = createServices()): void {
  app.provide(appConfigKey, services.config)
  app.provide(customerServiceKey, services.customers)
  app.provide(productServiceKey, services.products)
  app.provide(invoiceServiceKey, services.invoices)
  app.provide(invoiceLineServiceKey, services.invoiceLines)
  app.provide(invoicePdfGeneratorKey, services.invoicePdf)
}

/**
 * Fails loudly when a dependency was never provided. A missing injection is a wiring
 * bug, and surfacing it as `undefined` would only defer the crash to somewhere less
 * obvious.
 */
export function injectRequired<T>(key: InjectionKey<T>, description: string): T {
  const resolved = inject(key)

  if (resolved === undefined) {
    throw new Error(`${description} was not provided. Did installServices run?`)
  }

  return resolved
}
