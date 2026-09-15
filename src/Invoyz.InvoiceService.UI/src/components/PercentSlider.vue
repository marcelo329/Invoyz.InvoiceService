<script setup lang="ts">
import { computed } from 'vue'

/**
 * Edits a rate as a percentage while the model stays the 0–1 fraction the API expects.
 *
 * `nullable` supports invoice lines, where an absent rate is meaningful: it tells the
 * API to snapshot the product's current rate rather than pin one to the line.
 */
const props = withDefaults(
  defineProps<{
    /** Fraction in the 0–1 range, or null when nullable and unset. */
    modelValue: number | null
    nullable?: boolean
    /** Step in percentage points. */
    step?: number
    nullLabel?: string
  }>(),
  {
    nullable: false,
    step: 0.5,
    nullLabel: "Use the product's current rate",
  },
)

const emit = defineEmits<{ 'update:modelValue': [value: number | null] }>()

const isUnset = computed(() => props.modelValue === null || props.modelValue === undefined)

// Rounded on both conversions: 0.23 * 100 is 23.000000000000004 in binary floating
// point, and sending that back would fail the API's 0–1 range check on the boundary.
const percent = computed(() => (isUnset.value ? 0 : round(props.modelValue! * 100, 2)))

function round(value: number, decimals: number): number {
  return Number(value.toFixed(decimals))
}

function onPercentInput(event: Event): void {
  const raw = Number((event.target as HTMLInputElement).value)

  emit('update:modelValue', round(raw / 100, 4))
}

function onToggleUnset(event: Event): void {
  const useProductRate = (event.target as HTMLInputElement).checked

  emit('update:modelValue', useProductRate ? null : 0)
}
</script>

<template>
  <div class="percent">
    <label v-if="nullable" class="percent__toggle">
      <input type="checkbox" :checked="isUnset" @change="onToggleUnset" />
      <span>{{ nullLabel }}</span>
    </label>

    <div class="percent__control" :class="{ 'percent__control--disabled': isUnset }">
      <input
        type="range"
        min="0"
        max="100"
        :step="step"
        :value="percent"
        :disabled="isUnset"
        :aria-label="`Tax rate percent`"
        @input="onPercentInput"
      />
      <output class="percent__value">{{ isUnset ? '—' : `${percent}%` }}</output>
    </div>
  </div>
</template>

<style scoped>
.percent {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.percent__toggle {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  font-size: 0.85rem;
  color: var(--muted);
  cursor: pointer;
}
.percent__toggle input {
  width: auto;
  margin: 0;
}
.percent__control {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
.percent__control--disabled {
  opacity: 0.5;
}
.percent__control input[type='range'] {
  flex: 1;
  padding: 0;
  border: none;
  background: transparent;
  accent-color: var(--primary);
  cursor: pointer;
}
.percent__control input[type='range']:disabled {
  cursor: not-allowed;
}
.percent__value {
  min-width: 4.25rem;
  text-align: right;
  font-variant-numeric: tabular-nums;
  font-weight: 600;
}
</style>
