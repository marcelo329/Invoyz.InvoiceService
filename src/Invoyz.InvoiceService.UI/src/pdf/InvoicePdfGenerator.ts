import type { Invoice } from '@/domain/models'

/**
 * Strategy seam for invoice PDF generation.
 *
 * The generation itself is not written yet. Defining the port now means the button,
 * the wiring and the error handling are all real and exercised; supplying a working
 * implementation later is a one-line change at the composition root, with nothing in
 * the view or the service layer to revisit.
 */
export interface InvoicePdfGenerator {
  /** Whether this implementation can currently produce a document. */
  readonly isAvailable: boolean

  /**
   * Produces the invoice as a PDF. Implementations should resolve once the document
   * has been handed to the user (downloaded, opened, or saved).
   */
  generate(invoice: Invoice): Promise<void>
}

export class PdfGenerationNotImplementedError extends Error {
  constructor() {
    super('PDF generation has not been implemented yet.')
    this.name = 'PdfGenerationNotImplementedError'
  }
}

/**
 * Null-object implementation: keeps the application wired end to end while the real
 * generator is outstanding. It reports itself unavailable so the UI can explain the
 * situation instead of appearing broken, and still throws if invoked, so nobody can
 * mistake silence for success.
 */
export class NotImplementedInvoicePdfGenerator implements InvoicePdfGenerator {
  readonly isAvailable = false

  async generate(invoice: Invoice): Promise<void> {
    void invoice
    throw new PdfGenerationNotImplementedError()
  }
}
