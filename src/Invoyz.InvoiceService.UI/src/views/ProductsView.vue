<script setup lang="ts">
import { onMounted, ref } from 'vue'

import ConfirmDialog from '@/components/ConfirmDialog.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import FormField from '@/components/FormField.vue'
import ModalDialog from '@/components/ModalDialog.vue'
import PagerBar from '@/components/PagerBar.vue'
import PercentSlider from '@/components/PercentSlider.vue'
import ResourceTable, { type TableColumn } from '@/components/ResourceTable.vue'
import { useCrudList } from '@/composables/useCrudList'
import { useDirtyForm } from '@/composables/useDirtyForm'
import { appConfigKey, injectRequired, productServiceKey } from '@/core/container'
import { formatDate, formatMoney, formatPercent } from '@/core/format'
import type { Product, ProductWriteModel } from '@/domain/models'

const config = injectRequired(appConfigKey, 'App config')
const service = injectRequired(productServiceKey, 'Product service')

const list = useCrudList<Product, ProductWriteModel, ProductWriteModel>(
  service,
  config.ui.defaultPageSize,
)

const columns: TableColumn<Product>[] = [
  { key: 'name', label: 'Name' },
  { key: 'description', label: 'Description' },
  { key: 'unitPrice', label: 'Unit price', align: 'right', format: (row) => formatMoney(row.unitPrice) },
  { key: 'taxRate', label: 'Tax rate', align: 'right', format: (row) => formatPercent(row.taxRate) },
  { key: 'createdAt', label: 'Created', format: (row) => formatDate(row.createdAt) },
]

const formOpen = ref(false)
const editingId = ref<string | null>(null)
const form = ref<ProductWriteModel>(emptyForm())
const pendingDelete = ref<Product | null>(null)

const { isDirty, markPristine } = useDirtyForm(() => form.value)

function emptyForm(): ProductWriteModel {
  return { name: '', description: '', unitPrice: 0, taxRate: 0 }
}

function openCreate(): void {
  editingId.value = null
  form.value = emptyForm()
  markPristine()
  list.clearError()
  formOpen.value = true
}

function openEdit(product: Product): void {
  editingId.value = product.id
  form.value = {
    name: product.name,
    description: product.description,
    unitPrice: product.unitPrice,
    taxRate: product.taxRate,
  }
  markPristine()
  list.clearError()
  formOpen.value = true
}

async function submit(): Promise<void> {
  const payload: ProductWriteModel = {
    ...form.value,
    unitPrice: Number(form.value.unitPrice),
    taxRate: Number(form.value.taxRate),
  }

  const succeeded = editingId.value
    ? await list.update(editingId.value, payload)
    : await list.create(payload)

  if (succeeded) {
    formOpen.value = false
  }
}

function requestDelete(product: Product): void {
  list.clearError()
  pendingDelete.value = product
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

onMounted(list.refresh)
</script>

<template>
  <section>
    <header class="page-header">
      <div>
        <h1>Products</h1>
        <p class="page-header__hint">
          Tax rate is set as a percentage and stored as a fraction.
        </p>
      </div>
      <button type="button" class="btn btn--primary" @click="openCreate">New product</button>
    </header>

    <ErrorAlert
      v-if="!formOpen && !pendingDelete"
      :error="list.error.value"
      @dismiss="list.clearError"
    />

    <ResourceTable
      :columns="columns"
      :rows="list.items.value"
      :loading="list.loading.value"
      empty-message="No products yet."
      @edit="openEdit"
      @remove="requestDelete"
    />

    <PagerBar
      :page="list.page.value"
      :has-next-page="list.hasNextPage.value"
      :loading="list.loading.value"
      @change="list.goToPage"
    />

    <ModalDialog
      :open="formOpen"
      :title="editingId ? 'Edit product' : 'New product'"
      :busy="list.saving.value"
      :dirty="isDirty"
      @close="formOpen = false"
    >
      <ErrorAlert :error="list.error.value" @dismiss="list.clearError" />

      <form @submit.prevent="submit">
        <FormField label="Name" :errors="list.fieldErrors.value.Name">
          <input v-model="form.name" type="text" required />
        </FormField>
        <FormField label="Description" :errors="list.fieldErrors.value.Description">
          <input v-model="form.description" type="text" required />
        </FormField>
        <FormField label="Unit price" :errors="list.fieldErrors.value.UnitPrice">
          <input v-model.number="form.unitPrice" type="number" min="0" step="0.01" required />
        </FormField>
        <FormField label="Tax rate" :errors="list.fieldErrors.value.TaxRate">
          <PercentSlider v-model="form.taxRate" />
        </FormField>
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
      title="Delete product"
      :message="`Delete ${pendingDelete?.name}? A product used by an invoice line cannot be deleted.`"
      :busy="list.saving.value"
      :error="list.error.value"
      @cancel="cancelDelete"
      @confirm="confirmDelete"
      @dismiss-error="list.clearError"
    />
  </section>
</template>
