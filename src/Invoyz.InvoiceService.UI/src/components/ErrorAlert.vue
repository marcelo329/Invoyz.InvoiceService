<script setup lang="ts">
import type { ApiError } from '@/core/errors/ApiError'

defineProps<{ error: ApiError | null }>()
defineEmits<{ dismiss: [] }>()
</script>

<template>
  <div v-if="error" class="alert" role="alert">
    <div class="alert__body">
      <strong>{{
        error.isValidation
          ? 'Check the form'
          : error.isConflict
            ? 'Conflict'
            : error.isNotFound
              ? 'Not found'
              : 'Something went wrong'
      }}</strong>
      <p>{{ error.message }}</p>
    </div>
    <button type="button" class="alert__close" @click="$emit('dismiss')" aria-label="Dismiss">
      &times;
    </button>
  </div>
</template>

<style scoped>
.alert {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  border: 1px solid var(--danger-border);
  background: var(--danger-bg);
  color: var(--danger-fg);
  border-radius: 8px;
  padding: 0.75rem 1rem;
  margin-bottom: 1rem;
}
.alert__body {
  flex: 1;
}
.alert__body p {
  margin: 0.25rem 0 0;
}
.alert__close {
  background: none;
  border: none;
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
  color: inherit;
}
</style>
