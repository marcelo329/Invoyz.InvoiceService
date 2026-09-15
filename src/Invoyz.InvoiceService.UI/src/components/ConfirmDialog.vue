<script setup lang="ts">
import type { ApiError } from '@/core/errors/ApiError'
import ErrorAlert from './ErrorAlert.vue'
import ModalDialog from './ModalDialog.vue'

defineProps<{
  open: boolean
  title: string
  message: string
  busy?: boolean
  /**
   * A refused delete has to report itself here: the caller keeps this dialog open on
   * failure, and the page-level alert is hidden while it is up.
   */
  error?: ApiError | null
}>()

defineEmits<{ confirm: []; cancel: []; 'dismiss-error': [] }>()
</script>

<template>
  <ModalDialog :open="open" :title="title" :busy="busy" @close="$emit('cancel')">
    <ErrorAlert :error="error ?? null" @dismiss="$emit('dismiss-error')" />
    <p>{{ message }}</p>
    <template #footer>
      <button type="button" class="btn" :disabled="busy" @click="$emit('cancel')">Cancel</button>
      <button
        type="button"
        class="btn btn--danger"
        :disabled="busy"
        @click="$emit('confirm')"
      >
        {{ busy ? 'Deleting...' : 'Delete' }}
      </button>
    </template>
  </ModalDialog>
</template>
