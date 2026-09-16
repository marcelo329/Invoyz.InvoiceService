import axios, { AxiosError, type AxiosInstance } from 'axios'

import type { ApiConfig } from '@/config/appConfig'
import {
  ApiError,
  describeProblem,
  kindForStatus,
  type ProblemDetails,
} from '@/core/errors/ApiError'
import type { BinaryResponse, HttpClient, HttpRequest } from './HttpClient'

/**
 * Reads the suggested file name out of `Content-Disposition`, preferring the RFC 5987
 * `filename*` form when present so non-ASCII names survive.
 */
function fileNameFromContentDisposition(header: unknown): string | null {
  if (typeof header !== 'string') {
    return null
  }

  const encoded = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(header)

  if (encoded?.[1]) {
    try {
      return decodeURIComponent(encoded[1].trim().replace(/^"|"$/g, ''))
    } catch {
      // Malformed encoding: fall back to the plain form below.
    }
  }

  const plain = /filename="?([^";]+)"?/i.exec(header)

  return plain?.[1]?.trim() ?? null
}

/**
 * Adapter: the only file in the application that knows Axios exists.
 *
 * Its whole job is to turn Axios concerns (instances, interceptors, AxiosError) into
 * the transport-neutral `HttpClient` contract and `ApiError` failures.
 */
export class AxiosHttpClient implements HttpClient {
  private readonly instance: AxiosInstance

  constructor(config: ApiConfig, instance?: AxiosInstance) {
    this.instance =
      instance ??
      axios.create({
        baseURL: config.baseUrl,
        timeout: config.timeoutMs,
        headers: { 'Content-Type': 'application/json' },
      })
  }

  async get<TResponse>(request: HttpRequest): Promise<TResponse> {
    return this.send(() =>
      this.instance.get<TResponse>(request.url, {
        params: request.params,
        signal: request.signal,
      }),
    )
  }

  async post<TResponse, TBody>(request: HttpRequest, body: TBody): Promise<TResponse> {
    return this.send(() =>
      this.instance.post<TResponse>(request.url, body, {
        params: request.params,
        signal: request.signal,
      }),
    )
  }

  async put<TBody>(request: HttpRequest, body: TBody): Promise<void> {
    await this.send(() =>
      this.instance.put(request.url, body, {
        params: request.params,
        signal: request.signal,
      }),
    )
  }

  async delete(request: HttpRequest): Promise<void> {
    await this.send(() =>
      this.instance.delete(request.url, {
        params: request.params,
        signal: request.signal,
      }),
    )
  }

  async getBinary(request: HttpRequest): Promise<BinaryResponse> {
    try {
      const response = await this.instance.get<Blob>(request.url, {
        params: request.params,
        signal: request.signal,
        responseType: 'blob',
      })

      return {
        blob: response.data,
        fileName: fileNameFromContentDisposition(response.headers['content-disposition']),
      }
    } catch (error) {
      // With responseType 'blob' an error body arrives as a Blob too, so the usual
      // synchronous translation would report "[object Blob]" instead of the reason.
      throw await this.translateBinaryFailure(error)
    }
  }

  private async translateBinaryFailure(error: unknown): Promise<ApiError> {
    if (!axios.isAxiosError(error) || !error.response) {
      return this.translate(error)
    }

    const { status, data } = error.response

    if (data instanceof Blob) {
      try {
        const problem = JSON.parse(await data.text()) as ProblemDetails

        return new ApiError(
          kindForStatus(status),
          describeProblem(problem, `Request failed with status ${status}.`),
          status,
          problem?.errors ?? {},
        )
      } catch {
        // Not JSON — fall through to the generic message below.
      }
    }

    return new ApiError(
      kindForStatus(status),
      `Request failed with status ${status}.`,
      status,
    )
  }

  private async send<TResponse>(
    call: () => Promise<{ data: TResponse }>,
  ): Promise<TResponse> {
    try {
      const response = await call()
      return response.data
    } catch (error) {
      throw this.translate(error)
    }
  }

  private translate(error: unknown): ApiError {
    if (!axios.isAxiosError(error)) {
      return new ApiError('unexpected', error instanceof Error ? error.message : 'Unknown error')
    }

    const axiosError = error as AxiosError<ProblemDetails>
    const response = axiosError.response

    // No response at all: the API is down, the proxy is misconfigured, or the
    // request was aborted. Worth distinguishing, because retrying may help.
    if (!response) {
      return new ApiError('network', 'The API could not be reached. Is it running?')
    }

    const problem = response.data
    const status = response.status

    return new ApiError(
      kindForStatus(status),
      describeProblem(problem, `Request failed with status ${status}.`),
      status,
      problem?.errors ?? {},
    )
  }
}
