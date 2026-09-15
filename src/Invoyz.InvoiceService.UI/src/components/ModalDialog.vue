<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'

const props = defineProps<{
  open: boolean
  title: string
  busy?: boolean
  /** When true, dismissing asks before throwing the edits away. */
  dirty?: boolean
}>()

const emit = defineEmits<{ close: [] }>()

const confirmingDiscard = ref(false)

/**
 * Every dismissal route — backdrop, the close button, Escape — funnels through here,
 * so a half-filled form cannot be lost by any of them.
 */
function attemptClose(): void {
  if (props.busy) {
    return
  }

  if (props.dirty) {
    confirmingDiscard.value = true
    return
  }

  emit('close')
}

function discard(): void {
  confirmingDiscard.value = false
  emit('close')
}

function keepEditing(): void {
  confirmingDiscard.value = false
}

function onKeydown(event: KeyboardEvent): void {
  if (event.key !== 'Escape') {
    return
  }

  event.stopPropagation()

  // Escape backs out of the discard prompt first, rather than skipping past it.
  if (confirmingDiscard.value) {
    keepEditing()
    return
  }

  attemptClose()
}

watch(
  () => props.open,
  (open) => {
    confirmingDiscard.value = false

    if (open) {
      window.addEventListener('keydown', onKeydown)
    } else {
      window.removeEventListener('keydown', onKeydown)
    }
  },
  { immediate: true },
)

onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <div v-if="open" class="backdrop" @click.self="attemptClose">
    <div class="modal" role="dialog" aria-modal="true" :aria-label="title">
      <header class="modal__header">
        <h2>{{ title }}</h2>
        <button type="button" @click="attemptClose" :disabled="busy" aria-label="Close">
          &times;
        </button>
      </header>

      <div class="modal__body">
        <slot />
      </div>

      <footer class="modal__footer">
        <slot name="footer" />
      </footer>

      <div v-if="confirmingDiscard" class="discard" role="alertdialog" aria-label="Discard changes">
        <div class="discard__panel">
          <strong>Discard your changes?</strong>
          <p>This form has unsaved edits.</p>
          <div class="discard__actions">
            <button type="button" class="btn" @click="keepEditing">Keep editing</button>
            <button type="button" class="btn btn--danger" @click="discard">Discard</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.backdrop {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  z-index: 50;
}
.modal {
  position: relative;
  background: var(--surface);
  border-radius: 12px;
  box-shadow: 0 20px 45px rgba(15, 23, 42, 0.25);
  width: min(560px, 100%);
  max-height: 90vh;
  display: flex;
  flex-direction: column;
}
.modal__header,
.modal__footer {
  padding: 1rem 1.25rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}
.modal__header {
  border-bottom: 1px solid var(--border);
}
.modal__header h2 {
  margin: 0;
  font-size: 1.05rem;
}
.modal__header button {
  background: none;
  border: none;
  font-size: 1.4rem;
  line-height: 1;
  cursor: pointer;
  color: var(--muted);
}
.modal__body {
  padding: 1.25rem;
  overflow-y: auto;
}
.modal__footer {
  border-top: 1px solid var(--border);
  justify-content: flex-end;
}

.discard {
  position: absolute;
  inset: 0;
  background: rgba(15, 23, 42, 0.55);
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}
.discard__panel {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 1.1rem 1.25rem;
  max-width: 340px;
  text-align: left;
}
.discard__panel p {
  margin: 0.35rem 0 1rem;
  color: var(--muted);
}
.discard__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
</style>
