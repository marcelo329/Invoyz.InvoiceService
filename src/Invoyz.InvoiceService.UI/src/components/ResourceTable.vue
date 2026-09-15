<script setup lang="ts" generic="TRow extends { id: string }">
export interface TableColumn<T> {
  key: string
  label: string
  align?: 'left' | 'right'
  format?: (row: T) => string
}

defineProps<{
  columns: TableColumn<TRow>[]
  rows: TRow[]
  loading?: boolean
  emptyMessage?: string
}>()

defineEmits<{ edit: [row: TRow]; remove: [row: TRow] }>()
</script>

<template>
  <div class="table-wrap">
    <table>
      <thead>
        <tr>
          <th v-for="column in columns" :key="column.key" :class="column.align ?? 'left'">
            {{ column.label }}
          </th>
          <th class="right">Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-if="loading">
          <td :colspan="columns.length + 1" class="state">Loading...</td>
        </tr>
        <tr v-else-if="rows.length === 0">
          <td :colspan="columns.length + 1" class="state">
            {{ emptyMessage ?? 'Nothing here yet.' }}
          </td>
        </tr>
        <tr v-for="row in rows" :key="row.id" v-else>
          <td v-for="column in columns" :key="column.key" :class="column.align ?? 'left'">
            {{ column.format ? column.format(row) : ((row as never)[column.key] ?? '') }}
          </td>
          <td class="right actions">
            <slot name="row-actions" :row="row" />
            <button type="button" class="btn btn--small" @click="$emit('edit', row)">Edit</button>
            <button type="button" class="btn btn--small btn--danger" @click="$emit('remove', row)">
              Delete
            </button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.table-wrap {
  overflow-x: auto;
  border: 1px solid var(--border);
  border-radius: 10px;
  background: var(--surface);
}
table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.925rem;
}
th,
td {
  padding: 0.65rem 0.9rem;
  border-bottom: 1px solid var(--border);
  white-space: nowrap;
}
th {
  background: var(--surface-alt);
  font-weight: 600;
  color: var(--muted);
  text-transform: uppercase;
  font-size: 0.72rem;
  letter-spacing: 0.04em;
}
tbody tr:last-child td {
  border-bottom: none;
}
.right {
  text-align: right;
}
.left {
  text-align: left;
}
.state {
  text-align: center;
  color: var(--muted);
  padding: 1.75rem;
}
.actions {
  display: flex;
  gap: 0.4rem;
  justify-content: flex-end;
}
</style>
