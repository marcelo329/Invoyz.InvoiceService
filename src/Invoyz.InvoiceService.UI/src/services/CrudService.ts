import type { HttpClient } from '@/core/http/HttpClient'
import type { AuditedResource, Guid, PageRequest } from '@/domain/models'

/**
 * Capabilities are declared separately rather than as one fat interface (ISP), so a
 * consumer that only lists can depend on `ReadableRepository` alone. `CrudRepository`
 * simply composes them for the common case.
 */
export interface ReadableRepository<TResource> {
  list(page: PageRequest, signal?: AbortSignal): Promise<TResource[]>
  getById(id: Guid, signal?: AbortSignal): Promise<TResource>
}

export interface WritableRepository<TCreate, TUpdate> {
  create(model: TCreate, signal?: AbortSignal): Promise<Guid>
  update(id: Guid, model: TUpdate, signal?: AbortSignal): Promise<void>
  remove(id: Guid, signal?: AbortSignal): Promise<void>
}

export interface CrudRepository<TResource, TCreate, TUpdate>
  extends ReadableRepository<TResource>,
    WritableRepository<TCreate, TUpdate> {}

/**
 * Every endpoint in this API follows the same five-verb shape, so the behaviour lives
 * here once and each resource supplies only its base path (OCP: extend by subclassing
 * or by passing a different path, never by editing this).
 *
 * POST returns 201 with a `Location` header rather than a body, so `create` recovers
 * the new id from that header.
 */
export class HttpCrudRepository<TResource extends AuditedResource, TCreate, TUpdate>
  implements CrudRepository<TResource, TCreate, TUpdate>
{
  constructor(
    protected readonly http: HttpClient,
    protected readonly basePath: string,
  ) {}

  list(page: PageRequest, signal?: AbortSignal): Promise<TResource[]> {
    return this.http.get<TResource[]>({
      url: this.basePath,
      params: { page: page.page, pageSize: page.pageSize },
      signal,
    })
  }

  getById(id: Guid, signal?: AbortSignal): Promise<TResource> {
    return this.http.get<TResource>({ url: `${this.basePath}/${id}`, signal })
  }

  async create(model: TCreate, signal?: AbortSignal): Promise<Guid> {
    const created = await this.http.post<{ id?: Guid } | Guid, TCreate>(
      { url: this.basePath, signal },
      model,
    )

    // The API returns the raw Guid as the body; tolerate an object shape too so a
    // later change to `Created(uri, new { id })` does not break the client.
    if (typeof created === 'string') {
      return created
    }

    return created?.id ?? ''
  }

  update(id: Guid, model: TUpdate, signal?: AbortSignal): Promise<void> {
    return this.http.put<TUpdate>({ url: `${this.basePath}/${id}`, signal }, model)
  }

  remove(id: Guid, signal?: AbortSignal): Promise<void> {
    return this.http.delete({ url: `${this.basePath}/${id}`, signal })
  }
}
