import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  { path: '/', redirect: { name: 'customers' } },
  {
    path: '/customers',
    name: 'customers',
    component: () => import('@/views/CustomersView.vue'),
    meta: { title: 'Customers' },
  },
  {
    path: '/products',
    name: 'products',
    component: () => import('@/views/ProductsView.vue'),
    meta: { title: 'Products' },
  },
  {
    path: '/invoices',
    name: 'invoices',
    component: () => import('@/views/InvoicesView.vue'),
    meta: { title: 'Invoices' },
  },
  {
    // props: true hands the route id to the view as a prop, so the component never
    // reaches into the router and stays trivially testable.
    path: '/invoices/:id',
    name: 'invoice-detail',
    component: () => import('@/views/InvoiceDetailView.vue'),
    props: true,
    meta: { title: 'Invoice' },
  },
  { path: '/:pathMatch(.*)*', redirect: { name: 'customers' } },
]

export function createAppRouter(appTitle: string) {
  const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
  })

  router.afterEach((to) => {
    const section = typeof to.meta.title === 'string' ? to.meta.title : ''
    document.title = section ? `${section} · ${appTitle}` : appTitle
  })

  return router
}
