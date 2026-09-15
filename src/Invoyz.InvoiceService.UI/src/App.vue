<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'

import { appConfigKey, injectRequired } from '@/core/container'

const config = injectRequired(appConfigKey, 'App config')

const sections = [
  { name: 'customers', label: 'Customers' },
  { name: 'products', label: 'Products' },
  { name: 'invoices', label: 'Invoices' },
] as const
</script>

<template>
  <div class="shell">
    <aside class="sidebar">
      <div class="brand">{{ config.ui.appTitle }}</div>
      <nav>
        <RouterLink
          v-for="section in sections"
          :key="section.name"
          :to="{ name: section.name }"
          class="nav-link"
        >
          {{ section.label }}
        </RouterLink>
      </nav>
    </aside>

    <main class="content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.shell {
  display: grid;
  grid-template-columns: 220px 1fr;
  min-height: 100vh;
}
.sidebar {
  background: var(--sidebar);
  color: var(--sidebar-fg);
  padding: 1.5rem 1rem;
}
.brand {
  font-weight: 700;
  font-size: 1.1rem;
  letter-spacing: 0.02em;
  margin-bottom: 1.5rem;
  padding-left: 0.65rem;
}
nav {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.nav-link {
  color: inherit;
  text-decoration: none;
  padding: 0.55rem 0.65rem;
  border-radius: 7px;
  font-size: 0.925rem;
  opacity: 0.78;
}
.nav-link:hover {
  background: rgba(255, 255, 255, 0.08);
  opacity: 1;
}
.nav-link.router-link-active {
  background: rgba(255, 255, 255, 0.14);
  opacity: 1;
  font-weight: 600;
}
.content {
  padding: 2rem 2.25rem;
  overflow-x: hidden;
}

@media (max-width: 720px) {
  .shell {
    grid-template-columns: 1fr;
  }
  .sidebar {
    padding: 1rem;
  }
  nav {
    flex-direction: row;
    flex-wrap: wrap;
  }
  .content {
    padding: 1.25rem 1rem;
  }
}
</style>
