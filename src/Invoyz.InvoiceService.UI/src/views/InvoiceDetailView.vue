<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

import ConfirmDialog from '@/components/ConfirmDialog.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import FormField from '@/components/FormField.vue'
import ModalDialog from '@/components/ModalDialog.vue'
import PercentSlider from '@/components/PercentSlider.vue'
import ResourceTable, { type TableColumn } from '@/components/ResourceTable.vue'
import { useCrudList } from '@/composables/useCrudList'
import { useDirtyForm } from '@/composables/useDirtyForm'
import { ApiError } from '@/core/errors/ApiError'
import {
  customerServiceKey,
  injectRequired,
  invoiceLineServiceKey,
  invoicePdfGeneratorKey,
  invoiceServiceKey,
  productServiceKey,
} from '@/core/container'
import { formatDate, formatMoney, formatPercent } from '@/core/format'
import type { Customer, Invoice, InvoiceLine, InvoiceLineWriteModel, Product } from '@/domain/models'

const props = defineProps<{ id: string }>()

const invoiceService = injectRequired(invoiceServiceKey, 'Invoice service')
const lineFactory = injectRequired(invoiceLineServiceKey, 'Invoice line service factory')
const productService = injectRequired(productServiceKey, 'Product service')
const customerService = injectRequired(customerServiceKey, 'Customer service')
const pdfGenerator = injectRequired(invoicePdfGeneratorKey, 'Invoice PDF generator')

// One repository instance bound to this invoice for the whole view.
const lineRepository = lineFactory.forInvoice(props.id)

// Lines are paged like any other resource, so they reuse the same composable. The
// page size is generous because an invoice rarely has many.
const lines = useCrudList<InvoiceLine, InvoiceLineWriteModel, InvoiceLineWriteModel>(
  lineRepository,
  100,
)

const invoice = ref<Invoice | null>(null)
const customer = ref<Customer | null>(null)
const products = ref<Product[]>([])
const loadError = ref<ApiError | null>(null)
const loadingInvoice = ref(false)

const pdfError = ref<string | null>(null)
const pdfBusy = ref(false)

const productNameById = computed(
  () => new Map(products.value.map((product) => [product.id, product.name])),
)

const columns: TableColumn<InvoiceLine>[] = [
  {
    key: 'productId',
    label: 'Product',
    format: (row) => productNameById.value.get(row.productId) ?? row.productId,
  },
  { key: 'quantity', label: 'Qty', align: 'right' },
  { key: 'unitPrice', label: 'Unit price', align: 'right', format: (row) => formatMoney(row.unitPrice) },
  { key: 'taxRate', label: 'Tax rate', align: 'right', format: (row) => formatPercent(row.taxRate) },
  { key: 'lineTotal', label: 'Line total', align: 'right', format: (row) => formatMoney(row.lineTotal) },
  { key: 'lineTax', label: 'Line tax', align: 'right', format: (row) => formatMoney(row.lineTax) },
]

const formOpen = ref(false)
const editingId = ref<string | null>(null)
const form = ref<InvoiceLineWriteModel>(emptyForm())
const pendingDelete = ref<InvoiceLine | null>(null)

const { isDirty, markPristine } = useDirtyForm(() => form.value)

function emptyForm(): InvoiceLineWriteModel {
  return { productId: '', quantity: 1, unitPrice: null, taxRate: null }
}

function openCreate(): void {
  editingId.value = null
  form.value = emptyForm()
  markPristine()
  lines.clearError()
  formOpen.value = true
}

function openEdit(line: InvoiceLine): void {
  editingId.value = line.id
  form.value = {
    productId: line.productId,
    quantity: line.quantity,
    unitPrice: line.unitPrice,
    taxRate: line.taxRate,
  }
  markPristine()
  lines.clearError()
  formOpen.value = true
}

async function submit(): Promise<void> {
  const payload: InvoiceLineWriteModel = {
    productId: form.value.productId,
    quantity: Number(form.value.quantity),
    unitPrice:
      form.value.unitPrice === null || form.value.unitPrice === undefined
        ? null
        : Number(form.value.unitPrice),
    taxRate:
      form.value.taxRate === null || form.value.taxRate === undefined
        ? null
        : Number(form.value.taxRate),
  }

  const succeeded = editingId.value
    ? await lines.update(editingId.value, payload)
    : await lines.create(payload)

  if (succeeded) {
    formOpen.value = false
    // Line changes move the invoice totals, so the header has to be re-read.
    await loadInvoice()
  }
}

function requestDelete(line: InvoiceLine): void {
  lines.clearError()
  pendingDelete.value = line
}

function cancelDelete(): void {
  pendingDelete.value = null
  lines.clearError()
}

async function confirmDelete(): Promise<void> {
  if (!pendingDelete.value) return

  const succeeded = await lines.remove(pendingDelete.value.id)

  // On failure the dialog stays open so the reason is visible next to the action:
  // deleting the last remaining line is refused with a 409.
  if (succeeded) {
    pendingDelete.value = null
    await loadInvoice()
  }
}

async function loadInvoice(): Promise<void> {
  loadingInvoice.value = true
  loadError.value = null

  try {
    const loaded = await invoiceService.getById(props.id)
    invoice.value = loaded

    try {
      customer.value = await customerService.getById(loaded.customerId)
    } catch {
      // A missing customer should not blank the whole invoice.
      customer.value = null
    }
  } catch (caught) {
    loadError.value =
      caught instanceof ApiError ? caught : new ApiError('unexpected', 'Failed to load the invoice.')
  } finally {
    loadingInvoice.value = false
  }
}

async function downloadPdf(): Promise<void> {
  pdfError.value = null
  pdfBusy.value = true

  try {
    if (!invoice.value) {
      return
    }

    await pdfGenerator.generate(invoice.value)
  } catch (caught) {
    pdfError.value = caught instanceof Error ? caught.message : 'PDF generation failed.'
  } finally {
    pdfBusy.value = false
  }
}

onMounted(async () => {
  await Promise.all([
    loadInvoice(),
    lines.refresh(),
    productService
      .list({ page: 1, pageSize: 200 })
      .then((loaded) => (products.value = loaded))
      .catch(() => (products.value = [])),
  ])
})
</script>

<template>
  <section>
    <header class="page-header">
      <div>
        <RouterLink class="back" :to="{ name: 'invoices' }">&larr; Invoices</RouterLink>
        <h1>{{ invoice?.invoiceNumber ?? 'Invoice' }}</h1>
        <p class="page-header__hint">
          {{ customer?.name ?? 'Unknown customer' }} &middot; {{ invoice?.status ?? '' }}
        </p>
      </div>
      <div class="header-actions">
        <button
          type="button"
          class="btn"
          :disabled="!invoice || pdfBusy"
          :title="
            pdfGenerator.isAvailable
              ? 'Download this invoice as a PDF'
              : 'PDF generation has not been implemented yet'
          "
          @click="downloadPdf"
        >
          {{ pdfBusy ? 'Preparing...' : 'Download PDF' }}
        </button>
        <button type="button" class="btn btn--primary" :disabled="!invoice" @click="openCreate">
          Add line
        </button>
      </div>
    </header>

    <p v-if="pdfError" class="notice notice--warn">{{ pdfError }}</p>

    <ErrorAlert :error="loadError" @dismiss="loadError = null" />

    <div v-if="invoice" class="summary">
      <div><span>Issued</span><strong>{{ formatDate(invoice.issueDate) }}</strong></div>
      <div><span>Due</span><strong>{{ formatDate(invoice.dueDate) }}</strong></div>
      <div><span>Subtotal</span><strong>{{ formatMoney(invoice.subTotal) }}</strong></div>
      <div><span>Tax</span><strong>{{ formatMoney(invoice.taxTotal) }}</strong></div>
      <div class="summary__total">
        <span>Total</span><strong>{{ formatMoney(invoice.grandTotal) }}</strong>
      </div>
    </div>
    <p v-else-if="loadingInvoice" class="notice">Loading invoice...</p>

    <h2 class="section-title">Lines</h2>

    <ErrorAlert
      v-if="!formOpen && !pendingDelete"
      :error="lines.error.value"
      @dismiss="lines.clearError"
    />

    <ResourceTable
      :columns="columns"
      :rows="lines.items.value"
      :loading="lines.loading.value"
      empty-message="This invoice has no lines."
      @edit="openEdit"
      @remove="requestDelete"
    />

    <ModalDialog
      :open="formOpen"
      :title="editingId ? 'Edit line' : 'Add line'"
      :busy="lines.saving.value"
      :dirty="isDirty"
      @close="formOpen = false"
    >
      <ErrorAlert :error="lines.error.value" @dismiss="lines.clearError" />

      <form @submit.prevent="submit">
        <FormField label="Product" :errors="lines.fieldErrors.value.ProductId">
          <select v-model="form.productId" required>
            <option value="" disabled>Select a product</option>
            <option v-for="product in products" :key="product.id" :value="product.id">
              {{ product.name }}
            </option>
          </select>
        </FormField>
        <FormField label="Quantity" :errors="lines.fieldErrors.value.Quantity">
          <input v-model.number="form.quantity" type="number" min="1" step="1" required />
        </FormField>
        <FormField
          label="Unit price"
          hint="Leave empty to use the current product price"
          :errors="lines.fieldErrors.value.UnitPrice"
        >
          <input v-model.number="form.unitPrice" type="number" min="0" step="0.01" />
        </FormField>
        <FormField label="Tax rate" :errors="lines.fieldErrors.value.TaxRate">
          <PercentSlider v-model="form.taxRate" nullable />
        </FormField>
      </form>

      <template #footer>
        <button type="button" class="btn" :disabled="lines.saving.value" @click="formOpen = false">
          Cancel
        </button>
        <button
          type="button"
          class="btn btn--primary"
          :disabled="lines.saving.value"
          @click="submit"
        >
          {{ lines.saving.value ? 'Saving...' : 'Save' }}
        </button>
      </template>
    </ModalDialog>

    <ConfirmDialog
      :open="pendingDelete !== null"
      title="Delete line"
      message="Delete this line? An invoice must keep at least one line."
      :busy="lines.saving.value"
      :error="lines.error.value"
      @cancel="cancelDelete"
      @confirm="confirmDelete"
      @dismiss-error="lines.clearError"
    />
  </section>
</template>

<style scoped>
.back {
  font-size: 0.85rem;
  color: var(--muted);
  text-decoration: none;
}
.header-actions {
  display: flex;
  gap: 0.5rem;
}
.summary {
  display: flex;
  flex-wrap: wrap;
  gap: 1.5rem;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 1rem 1.25rem;
  margin-bottom: 1.5rem;
}
.summary div {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}
.summary span {
  font-size: 0.72rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--muted);
}
.summary__total strong {
  font-size: 1.15rem;
}
.section-title {
  font-size: 1rem;
  margin: 0 0 0.75rem;
}
.notice {
  background: var(--surface-alt);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 0.6rem 0.9rem;
  color: var(--muted);
}
.notice--warn {
  border-color: var(--danger-border);
  background: var(--danger-bg);
  color: var(--danger-fg);
  margin-bottom: 1rem;
}
</style>
