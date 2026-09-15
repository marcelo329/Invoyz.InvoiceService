import { ref, shallowRef, type Ref } from 'vue'

import { ApiError } from '@/core/errors/ApiError'
import type { CrudRepository } from '@/services/CrudService'
import type { AuditedResource, Guid, PageRequest } from '@/domain/models'

export interface CrudListState<TResource> {
  readonly items: Ref<TResource[]>
  readonly loading: Ref<boolean>
  readonly saving: Ref<boolean>
  readonly error: Ref<ApiError | null>
  readonly fieldErrors: Ref<Record<string, string[]>>
  readonly page: Ref<number>
  readonly pageSize: Ref<number>
  readonly hasNextPage: Ref<boolean>
  refresh(): Promise<void>
  goToPage(page: number): Promise<void>
  create(model: unknown): Promise<boolean>
  update(id: Guid, model: unknown): Promise<boolean>
  remove(id: Guid): Promise<boolean>
  clearError(): void
}

/**
 * The list/save/delete cycle every resource view repeats, in one place.
 *
 * Views own presentation; this owns request state and error translation. Keeping them
 * apart is what lets all four resources share a single table and a single set of
 * loading and error affordances.
 */
export function useCrudList<TResource extends AuditedResource, TCreate, TUpdate>(
  repository: CrudRepository<TResource, TCreate, TUpdate>,
  defaultPageSize: number,
): CrudListState<TResource> {
  const items = shallowRef<TResource[]>([]) as Ref<TResource[]>
  const loading = ref(false)
  const saving = ref(false)
  const error = ref<ApiError | null>(null)
  const fieldErrors = ref<Record<string, string[]>>({})
  const page = ref(1)
  const pageSize = ref(defaultPageSize)
  const hasNextPage = ref(false)

  function clearError(): void {
    error.value = null
    fieldErrors.value = {}
  }

  function capture(caught: unknown): void {
    const apiError =
      caught instanceof ApiError
        ? caught
        : new ApiError('unexpected', caught instanceof Error ? caught.message : 'Unknown error')

    error.value = apiError
    fieldErrors.value = { ...apiError.fieldErrors }
  }

  async function refresh(): Promise<void> {
    loading.value = true
    clearError()

    try {
      const request: PageRequest = { page: page.value, pageSize: pageSize.value }
      const result = await repository.list(request)

      items.value = result
      // The API returns a bare array with no total count, so a full page is the only
      // available signal that another page may exist.
      hasNextPage.value = result.length === pageSize.value
    } catch (caught) {
      capture(caught)
      items.value = []
      hasNextPage.value = false
    } finally {
      loading.value = false
    }
  }

  async function goToPage(target: number): Promise<void> {
    // page=0 is accepted by the API but silently returns page 1, so clamp here.
    page.value = Math.max(1, target)
    await refresh()
  }

  async function mutate(action: () => Promise<unknown>): Promise<boolean> {
    saving.value = true
    clearError()

    try {
      await action()
      await refresh()
      return true
    } catch (caught) {
      capture(caught)
      return false
    } finally {
      saving.value = false
    }
  }

  return {
    items,
    loading,
    saving,
    error,
    fieldErrors,
    page,
    pageSize,
    hasNextPage,
    refresh,
    goToPage,
    clearError,
    create: (model) => mutate(() => repository.create(model as TCreate)),
    update: (id, model) => mutate(() => repository.update(id, model as TUpdate)),
    remove: (id) => mutate(() => repository.remove(id)),
  }
}
