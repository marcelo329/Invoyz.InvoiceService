<script setup lang="ts">
import { onMounted, ref } from 'vue'

import ConfirmDialog from '@/components/ConfirmDialog.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import FormField from '@/components/FormField.vue'
import ModalDialog from '@/components/ModalDialog.vue'
import PagerBar from '@/components/PagerBar.vue'
import ResourceTable, { type TableColumn } from '@/components/ResourceTable.vue'
import { useCrudList } from '@/composables/useCrudList'
import { useDirtyForm } from '@/composables/useDirtyForm'
import { appConfigKey, customerServiceKey, injectRequired } from '@/core/container'
import type { Customer, CustomerWriteModel } from '@/domain/models'
import { formatDate } from '@/core/format'

const config = injectRequired(appConfigKey, 'App config')
const service = injectRequired(customerServiceKey, 'Customer service')

const list = useCrudList<Customer, CustomerWriteModel, CustomerWriteModel>(
  service,
  config.ui.defaultPageSize,
)

const columns: TableColumn<Customer>[] = [
  { key: 'name', label: 'Name' },
  { key: 'email', label: 'Email' },
  { key: 'vatNumber', label: 'VAT number' },
  { key: 'address', label: 'Address' },
  { key: 'createdAt', label: 'Created', format: (row) => formatDate(row.createdAt) },
]

const formOpen = ref(false)
const editingId = ref<string | null>(null)
const form = ref<CustomerWriteModel>(emptyForm())
const pendingDelete = ref<Customer | null>(null)

const { isDirty, markPristine } = useDirtyForm(() => form.value)

function emptyForm(): CustomerWriteModel {
  return { name: '', address: '', email: '', vatNumber: '' }
}

function openCreate(): void {
  editingId.value = null
  form.value = emptyForm()
  markPristine()
  list.clearError()
  formOpen.value = true
}

function openEdit(customer: Customer): void {
  editingId.value = customer.id
  form.value = {
    name: customer.name,
    address: customer.address,
    email: customer.email,
    vatNumber: customer.vatNumber,
  }
  markPristine()
  list.clearError()
  formOpen.value = true
}

async function submit(): Promise<void> {
  const succeeded = editingId.value
    ? await list.update(editingId.value, { ...form.value })
    : await list.create({ ...form.value })

  // The dialog stays open on failure so validation messages land next to the fields.
  if (succeeded) {
    formOpen.value = false
  }
}

function requestDelete(customer: Customer): void {
  list.clearError()
  pendingDelete.value = customer
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
        <h1>Customers</h1>
        <p class="page-header__hint">VAT numbers must be unique among active customers.</p>
      </div>
      <button type="button" class="btn btn--primary" @click="openCreate">New customer</button>
    </header>

    <ErrorAlert v-if="!formOpen && !pendingDelete" :error="list.error.value" @dismiss="list.clearError" />

    <ResourceTable
      :columns="columns"
      :rows="list.items.value"
      :loading="list.loading.value"
      empty-message="No customers yet."
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
      :title="editingId ? 'Edit customer' : 'New customer'"
      :busy="list.saving.value"
      :dirty="isDirty"
      @close="formOpen = false"
    >
      <ErrorAlert :error="list.error.value" @dismiss="list.clearError" />

      <form @submit.prevent="submit">
        <FormField label="Name" :errors="list.fieldErrors.value.Name">
          <input v-model="form.name" type="text" required />
        </FormField>
        <FormField label="Address" :errors="list.fieldErrors.value.Address">
          <input v-model="form.address" type="text" required />
        </FormField>
        <FormField label="Email" :errors="list.fieldErrors.value.Email">
          <input v-model="form.email" type="email" required />
        </FormField>
        <FormField label="VAT number" :errors="list.fieldErrors.value.VatNumber">
          <input v-model="form.vatNumber" type="text" required />
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
      title="Delete customer"
      :message="`Delete ${pendingDelete?.name}? This frees their VAT number for reuse.`"
      :busy="list.saving.value"
      :error="list.error.value"
      @cancel="cancelDelete"
      @confirm="confirmDelete"
      @dismiss-error="list.clearError"
    />
  </section>
</template>
