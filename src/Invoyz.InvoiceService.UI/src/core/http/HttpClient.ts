/**
 * The abstraction services depend on (DIP). Nothing above this file mentions Axios,
 * so the transport can be swapped — for a fake in tests, or fetch in a different
 * host — without touching a single service or component.
 */
export type QueryParams = Record<string, string | number | boolean | undefined>

export interface HttpRequest {
  readonly url: string
  readonly params?: QueryParams
  readonly signal?: AbortSignal
}

export interface HttpClient {
  get<TResponse>(request: HttpRequest): Promise<TResponse>
  post<TResponse, TBody>(request: HttpRequest, body: TBody): Promise<TResponse>
  put<TBody>(request: HttpRequest, body: TBody): Promise<void>
  delete(request: HttpRequest): Promise<void>
}
