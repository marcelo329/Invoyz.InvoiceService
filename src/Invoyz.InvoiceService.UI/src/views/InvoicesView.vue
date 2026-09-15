<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'

import ConfirmDialog from '@/components/ConfirmDialog.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import FormField from '@/components/FormField.vue'
import ModalDialog from '@/components/ModalDialog.vue'
import PagerBar from '@/components/PagerBar.vue'
import PercentSlider from '@/components/PercentSlider.vue'
import ResourceTable, { type TableColumn } from '@/components/ResourceTable.vue'
import { useCrudList } from '@/composables/useCrudList'
import { useDirtyForm } from '@/composables/useDirtyForm'
import {
  appConfigKey,
  customerServiceKey,
  injectRequired,
  invoiceServiceKey,
  productServiceKey,
} from '@/core/container'
import { formatDate, formatMoney, fromDateInputValue, toDateInputValue } from '@/core/format'
import type {
  Customer,
  Invoice,
  InvoiceCreateModel,
  InvoiceLineWriteModel,
  InvoiceUpdateModel,
  Product,
} from '@/domain/models'

const config = injectRequired(appConfigKey, 'App config')
const invoices = injectRequired(invoiceServiceKey, 'Invoice service')
const customerService = injectRequired(customerServiceKey, 'Customer service')
const productService = injectRequired(productServiceKey, 'Product service')

const list = useCrudList<Invoice, InvoiceCreateModel, InvoiceUpdateModel>(
  invoices,
  config.ui.defaultPageSize,
)

// Reference data for the pickers. A large catalogue would need a searchable control;
// a generous page size is enough at this scale.
const customers = ref<Customer[]>([])
const products = ref<Product[]>([])
const referenceDataError = ref<string | null>(null)

const customerNameById = computed(
  () => new Map(customers.value.map((customer) => [customer.id, customer.name])),
)

const columns: TableColumn<Invoice>[] = [
  { key: 'invoiceNumber', label: 'Number' },
  {
    key: 'customerId',
    label: 'Customer',
    format: (row) => customerNameById.value.get(row.customerId) ?? '—',
  },
  { key: 'status', label: 'Status' },
  { key: 'issueDate', label: 'Issued', format: (row) => formatDate(row.issueDate) },
  { key: 'dueDate', label: 'Due', format: (row) => formatDate(row.dueDate) },
  { key: 'grandTotal', label: 'Total', align: 'right', format: (row) => formatMoney(row.grandTotal) },
]

const formOpen = ref(false)
const editingId = ref<string | null>(null)
const pendingDelete = ref<Invoice | null>(null)

const createForm = ref<InvoiceCreateModel>(emptyCreateForm())
const updateForm = ref<InvoiceUpdateModel>(emptyUpdateForm())

// Two forms share one dialog, so dirtiness follows whichever is on screen.
const createDirty = useDirtyForm(() => createForm.value)
const updateDirty = useDirtyForm(() => updateForm.value)

const isDirty = computed(() =>
  editingId.value ? updateDirty.isDirty.value : createDirty.isDirty.value,
)

function markPristine(): void {
  createDirty.markPristine()
  updateDirty.markPristine()
}

function emptyCreateForm(): InvoiceCreateModel {
  const today = toDateInputValue(new Date())

  return {
    invoiceNumber: '',
    customerId: '',
    issueDate: today,
    dueDate: today,
    status: config.ui.invoiceStatuses[0] ?? 'Draft',
    lines: [newLine()],
  }
}

function emptyUpdateForm(): InvoiceUpdateModel {
  const today = toDateInputValue(new Date())

  return {
    invoiceNumber: '',
    customerId: '',
    issueDate: today,
    dueDate: today,
    status: config.ui.invoiceStatuses[0] ?? 'Draft',
  }
}

function newLine(): InvoiceLineWriteModel {
  return { productId: '', quantity: 1, unitPrice: null, taxRate: null }
}

function openCreate(): void {
  editingId.value = null
  createForm.value = emptyCreateForm()
  markPristine()
  list.clearError()
  formOpen.value = true
}

function openEdit(invoice: Invoice): void {
  editingId.value = invoice.id
  updateForm.value = {
    invoiceNumber: invoice.invoiceNumber,
    customerId: invoice.customerId,
    issueDate: toDateInputValue(invoice.issueDate),
    dueDate: toDateInputValue(invoice.dueDate),
    status: invoice.status,
  }
  markPristine()
  list.clearError()
  formOpen.value = true
}

function addLine(): void {
  createForm.value.lines.push(newLine())
}

function removeLine(index: number): void {
  createForm.value.lines.splice(index, 1)
}

async function submit(): Promise<void> {
  let succeeded: boolean

  if (editingId.value) {
    succeeded = await list.update(editingId.value, {
      ...updateForm.value,
      issueDate: fromDateInputValue(updateForm.value.issueDate),
      dueDate: fromDateInputValue(updateForm.value.dueDate),
    })
  } else {
    succeeded = await list.create({
      ...createForm.value,
      issueDate: fromDateInputValue(createForm.value.issueDate),
      dueDate: fromDateInputValue(createForm.value.dueDate),
      lines: createForm.value.lines.map((line) => ({
        productId: line.productId,
        quantity: Number(line.quantity),
        // Empty means "snapshot from the product", which the API expects as null.
        unitPrice: line.unitPrice === null || line.unitPrice === undefined ? null : Number(line.unitPrice),
        taxRate: line.taxRate === null || line.taxRate === undefined ? null : Number(line.taxRate),
      })),
    })
  }

  if (succeeded) {
    formOpen.value = false
  }
}

function requestDelete(invoice: Invoice): void {
  list.clearError()
  pendingDelete.value = invoice
}

function cancelDelete(): void {
  pendingDelete.value = null
  list.clearError()
}

async function confirmDelete(): Promise<void> {
  if (!pendingDelete.value) return

  const succeeded = await list.remove(pendingDelete.value.id)

  // On failure the dialog stays open so the reason is visible next to the action.
  if (succeeded) {
    pendingDelete.value = null
  }
}

async function loadReferenceData(): Promise<void> {
  try {
    const [loadedCustomers, loadedProducts] = await Promise.all([
      customerService.list({ page: 1, pageSize: 200 }),
      productService.list({ page: 1, pageSize: 200 }),
    ])

    customers.value = loadedCustomers
    products.value = loadedProducts
  } catch {
    referenceDataError.value =
      'Customers and products could not be loaded, so the pickers are empty.'
  }
}

onMounted(async () => {
  await Promise.all([list.refresh(), loadReferenceData()])
})
</script>

<template>
  <section>
    <header class="page-header">
      <div>
        <h1>Invoices</h1>
        <p class="page-header__hint">
          An invoice needs at least one line. Lines are managed from the invoice detail.
        </p>
      </div>
      <button type="button" class="btn btn--primary" @click="openCreate">New invoice</button>
    </header>

    <p v-if="referenceDataError" class="notice">{{ referenceDataError }}</p>

    <ErrorAlert
      v-if="!formOpen && !pendingDelete"
      :error="list.error.value"
      @dismiss="list.clearError"
    />

    <ResourceTable
      :columns="columns"
      :rows="list.items.value"
      :loading="list.loading.value"
      empty-message="No invoices yet."
      @edit="openEdit"
      @remove="requestDelete"
    >
      <template #row-actions="{ row }">
        <RouterLink class="btn btn--small" :to="{ name: 'invoice-detail', params: { id: row.id } }">
          Open
        </RouterLink>
      </template>
    </ResourceTable>

    <PagerBar
      :page="list.page.value"
      :has-next-page="list.hasNextPage.value"
      :loading="list.loading.value"
      @change="list.goToPage"
    />

    <ModalDialog
      :open="formOpen"
      :title="editingId ? 'Edit invoice' : 'New invoice'"
      :busy="list.saving.value"
      :dirty="isDirty"
      @close="formOpen = false"
    >
      <ErrorAlert :error="list.error.value" @dismiss="list.clearError" />

      <form v-if="editingId" @submit.prevent="submit">
        <FormField label="Invoice number" :errors="list.fieldErrors.value.InvoiceNumber">
          <input v-model="updateForm.invoiceNumber" type="text" required />
        </FormField>
        <FormField label="Customer" :errors="list.fieldErrors.value.CustomerId">
          <select v-model="updateForm.customerId" required>
            <option value="" disabled>Select a customer</option>
            <option v-for="customer in customers" :key="customer.id" :value="customer.id">
              {{ customer.name }}
            </option>
          </select>
        </FormField>
        <FormField label="Issue date" :errors="list.fieldErrors.value.IssueDate">
          <input v-model="updateForm.issueDate" type="date" required />
        </FormField>
        <FormField label="Due date" :errors="list.fieldErrors.value.DueDate">
          <input v-model="updateForm.dueDate" type="date" required />
        </FormField>
        <FormField label="Status" :errors="list.fieldErrors.value.Status">
          <select v-model="updateForm.status" required>
            <option v-for="status in config.ui.invoiceStatuses" :key="status" :value="status">
              {{ status }}
            </option>
          </select>
        </FormField>
      </form>

      <form v-else @submit.prevent="submit">
        <FormField label="Invoice number" :errors="list.fieldErrors.value.InvoiceNumber">
          <input v-model="createForm.invoiceNumber" type="text" required />
        </FormField>
        <FormField label="Customer" :errors="list.fieldErrors.value.CustomerId">
          <select v-model="createForm.customerId" required>
            <option value="" disabled>Select a customer</option>
            <option v-for="customer in customers" :key="customer.id" :value="customer.id">
              {{ customer.name }}
            </option>
          </select>
        </FormField>
        <FormField label="Issue date" :errors="list.fieldErrors.value.IssueDate">
          <input v-model="createForm.issueDate" type="date" required />
        </FormField>
        <FormField label="Due date" :errors="list.fieldErrors.value.DueDate">
          <input v-model="createForm.dueDate" type="date" required />
        </FormField>
        <FormField label="Status" :errors="list.fieldErrors.value.Status">
          <select v-model="createForm.status" required>
            <option v-for="status in config.ui.invoiceStatuses" :key="status" :value="status">
              {{ status }}
            </option>
          </select>
        </FormField>

        <fieldset class="lines">
          <legend>Lines</legend>
          <p class="lines__hint">
            Leave unit price empty, or keep the product rate, to snapshot them from the product.
          </p>

          <div v-for="(line, index) in createForm.lines" :key="index" class="lines__row">
            <div class="lines__fields">
              <select v-model="line.productId" required>
                <option value="" disabled>Product</option>
                <option v-for="product in products" :key="product.id" :value="product.id">
                  {{ product.name }}
                </option>
              </select>
              <input v-model.number="line.quantity" type="number" min="1" step="1" required placeholder="Qty" />
              <input v-model.number="line.unitPrice" type="number" min="0" step="0.01" placeholder="Unit price" />
              <button
                type="button"
                class="btn btn--small btn--danger"
                :disabled="createForm.lines.length === 1"
                @click="removeLine(index)"
              >
                &times;
              </button>
            </div>
            <PercentSlider v-model="line.taxRate" nullable />
          </div>

          <button type="button" class="btn btn--small" @click="addLine">Add line</button>
        </fieldset>
      </form>

      <template #footer>
        <button type="button" class="btn" :disabled="list.saving.value" @click="formOpen = false">
          Cancel
        </button>
        <button type="button" class="btn btn--primary" :disabled="list.saving.value" @click="submit">
          {{ list.saving.value ? 'Saving...' : 'Save' }}
        </button>
      </template>
    </ModalDialog>

    <ConfirmDialog
      :open="pendingDelete !== null"
      title="Delete invoice"
      :message="`Delete invoice ${pendingDelete?.invoiceNumber}? Its lines are deleted with it.`"
      :busy="list.saving.value"
      :error="list.error.value"
      @cancel="cancelDelete"
      @confirm="confirmDelete"
      @dismiss-error="list.clearError"
    />
  </section>
</template>

<style scoped>
.lines {
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 0.9rem;
  margin-top: 0.5rem;
}
.lines legend {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--muted);
  padding: 0 0.35rem;
}
.lines__hint {
  margin: 0 0 0.75rem;
  font-size: 0.8rem;
  color: var(--muted);
}
.lines__row {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  padding: 0.6rem;
  border: 1px solid var(--border);
  border-radius: 8px;
  margin-bottom: 0.6rem;
}
.lines__fields {
  display: grid;
  grid-template-columns: 2fr 0.8fr 1.1fr auto;
  gap: 0.4rem;
}
.notice {
  background: var(--surface-alt);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 0.6rem 0.9rem;
  color: var(--muted);
}
</style>
