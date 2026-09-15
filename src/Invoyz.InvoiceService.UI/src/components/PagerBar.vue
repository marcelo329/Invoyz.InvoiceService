<script setup lang="ts">
defineProps<{ page: number; hasNextPage: boolean; loading?: boolean }>()
defineEmits<{ change: [page: number] }>()
</script>

<template>
  <div class="pager">
    <button
      type="button"
      class="btn"
      :disabled="page <= 1 || loading"
      @click="$emit('change', page - 1)"
    >
      Previous
    </button>
    <span>Page {{ page }}</span>
    <button
      type="button"
      class="btn"
      :disabled="!hasNextPage || loading"
      @click="$emit('change', page + 1)"
    >
      Next
    </button>
  </div>
</template>

<style scoped>
.pager {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-top: 1rem;
}
</style>
