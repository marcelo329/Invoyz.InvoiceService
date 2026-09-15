import { computed, ref, type ComputedRef } from 'vue'

export interface DirtyFormState {
  /** True once the tracked value differs from the last pristine snapshot. */
  readonly isDirty: ComputedRef<boolean>
  /** Takes a new snapshot — call it whenever a form is (re)populated. */
  markPristine(): void
}

/**
 * Tracks whether a form still matches the values it was opened with.
 *
 * Structural comparison against a serialised snapshot is enough here: these forms are
 * small, flat, and JSON-shaped by construction, since they are sent to the API as-is.
 */
export function useDirtyForm<T>(source: () => T): DirtyFormState {
  const baseline = ref(snapshot())

  function snapshot(): string {
    return JSON.stringify(source())
  }

  return {
    isDirty: computed(() => snapshot() !== baseline.value),
    markPristine: () => {
      baseline.value = snapshot()
    },
  }
}
