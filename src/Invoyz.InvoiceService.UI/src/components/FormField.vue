<script setup lang="ts">
defineProps<{ label: string; errors?: string[]; hint?: string }>()
</script>

<template>
  <label class="field">
    <span class="field__label">{{ label }}</span>
    <slot />
    <small v-if="hint" class="field__hint">{{ hint }}</small>
    <small v-for="message in errors ?? []" :key="message" class="field__error">{{ message }}</small>
  </label>
</template>

<style scoped>
.field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  margin-bottom: 0.9rem;
}
.field__label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--muted);
}
.field__hint {
  color: var(--muted);
}
.field__error {
  color: var(--danger-fg);
}
</style>
